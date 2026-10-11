using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全域變數管理與 Session 狀態控制
/// 包含 PlayerPrefs 持久化邏輯
/// [已整理: 增加防呆檢查與註解]
/// </summary>
public static class GlobalVariables
{
    #region Public Fields

    // 注意：欄位名稱不可更改 (限制要求)
    public static string[] studentNames = new string[4];
    public static int[] playerCorrectCounts = new int[4]; // 紀錄每個玩家答對次數
    public static string currentSessionID;
    public const string DATABASE_URL = "https://quizflow-analytics-default-rtdb.asia-southeast1.firebasedatabase.app/";

    /// <summary>
    /// 暫存所有答題紀錄 (給結算圖表用 - 選用)
    /// </summary>
    public static List<AnswerData> localAnswerHistory = new List<AnswerData>();

    /// <summary>
    /// 目前選擇的難度等級 (0=未選, 1/2/3=難度)
    /// </summary>
    public static int selectedDifficultyLevel = 0;

    /// <summary>
    /// 用於避免重複題目的容器 (難度 -> 已出過的題目ID集合)
    /// </summary>
    public static Dictionary<int, HashSet<string>> usedQuestionIdsByDifficulty = new Dictionary<int, HashSet<string>>();

    /// <summary>
    /// 指定載入的題目 ID (若為空字串則載入全部)
    /// </summary>
    public static string selectedQuestionId = "";

    /// <summary>
    /// 目前選擇的玩家 Index (0-3)
    /// </summary>
    public static int currentPlayerIndex = -1;

    /// <summary>
    /// 玩家能量 (僅用於暫存，不顯示累積，依需求每題顯示該題可得能量)
    /// </summary>
    public static int energy = 0; 

    #endregion

    #region Session Management

    /// <summary>
    /// 檢查是否已登入 (Session ID 存在且至少有一個玩家名字)
    /// </summary>
    public static bool IsLoggedIn()
    {
        bool hasSession = !string.IsNullOrEmpty(currentSessionID);
        bool hasNames = studentNames != null && studentNames.Length > 0 && !string.IsNullOrEmpty(studentNames[0]);
        return hasSession && hasNames;
    }

    /// <summary>
    /// 儲存狀態至 PlayerPrefs
    /// </summary>
    public static void SaveState()
    {
        // 防呆：確保陣列不為空
        if (studentNames == null) studentNames = new string[4];
        if (playerCorrectCounts == null) playerCorrectCounts = new int[4];

        PlayerPrefs.SetString("CurrentSessionID", currentSessionID ?? "");
        for (int i = 0; i < 4; i++)
        {
            // 防止 IndexOutOfRange
            if (i < studentNames.Length) 
                PlayerPrefs.SetString($"PlayerName_{i}", studentNames[i] ?? "");
            
            if (i < playerCorrectCounts.Length) 
                PlayerPrefs.SetInt($"PlayerScore_{i}", playerCorrectCounts[i]);
        }
        PlayerPrefs.Save();
        Debug.Log("[GlobalVariables] State Saved.");
    }

    /// <summary>
    /// 從 PlayerPrefs 載入狀態
    /// </summary>
    public static void LoadState()
    {
        currentSessionID = PlayerPrefs.GetString("CurrentSessionID", "");
        
        if (studentNames == null || studentNames.Length != 4) studentNames = new string[4];
        if (playerCorrectCounts == null || playerCorrectCounts.Length != 4) playerCorrectCounts = new int[4];

        for (int i = 0; i < 4; i++)
        {
            studentNames[i] = PlayerPrefs.GetString($"PlayerName_{i}", "");
            playerCorrectCounts[i] = PlayerPrefs.GetInt($"PlayerScore_{i}", 0);
        }
        Debug.Log("[GlobalVariables] State Loaded.");
    }

    /// <summary>
    /// 清除所有狀態與 PlayerPrefs (登出用)
    /// </summary>
    public static void ClearState()
    {
        currentSessionID = "";
        studentNames = new string[4];
        playerCorrectCounts = new int[4];
        
        if (localAnswerHistory != null) localAnswerHistory.Clear();
        else localAnswerHistory = new List<AnswerData>();

        selectedQuestionId = "";
        selectedDifficultyLevel = 0;
        if (usedQuestionIdsByDifficulty != null) usedQuestionIdsByDifficulty.Clear();
        else usedQuestionIdsByDifficulty = new Dictionary<int, HashSet<string>>();

        currentPlayerIndex = -1;
        energy = 0;

        PlayerPrefs.DeleteKey("CurrentSessionID");
        for (int i = 0; i < 4; i++)
        {
            PlayerPrefs.DeleteKey($"PlayerName_{i}");
            PlayerPrefs.DeleteKey($"PlayerScore_{i}");
        }
        PlayerPrefs.Save();
        Debug.Log("[GlobalVariables] State Cleared.");
    }

    /// <summary>
    /// 重置遊戲資料 (相容舊方法)
    /// </summary>
    public static void ResetGameData()
    {
        ClearState();
    }

    #endregion
}