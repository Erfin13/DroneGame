using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using XCharts.Runtime; 
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

/// <summary>
/// 結算場景管理器 (XCharts 版本 - 含防誤觸)
/// [已整理: 資料載入防呆、UI 更新邏輯優化]
/// [Modified]: 新增 Firebase 資料讀取與統計顯示 (Fallback to local)
/// </summary>
public class ResultManager : MonoBehaviour
{
    [Header("XCharts 元件")]
    [Tooltip("請拖入場景中的 BarChart 物件")]
    public BarChart barChart;

    [Header("UI 元件")]
    [Tooltip("確定按鈕 (登出並回大廳)")]
    public Button confirmButton;

    [Tooltip("確認對話框 (需綁定)")]
    public ConfirmDialog confirmDialog;

    void Start()
    {
        // 確保載入最新分數 (Local fallback)
        GlobalVariables.LoadState();
        
        // 先顯示本機資料
        UpdateChartData();
        
        // 嘗試從 Firebase 讀取正式結果
        LoadResultsFromFirebase();

        if (confirmButton != null) 
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    /// <summary>
    /// [New] 從 Firebase 讀取該 Session 的完整答題紀錄
    /// </summary>
    private void LoadResultsFromFirebase()
    {
        string sessionID = GlobalVariables.currentSessionID;
        if (string.IsNullOrEmpty(sessionID))
        {
            Debug.LogWarning("[ResultManager] SessionID 為空，跳過 Firebase 載入");
            return;
        }

        string dbUrl = GlobalVariables.DATABASE_URL;
        
        // 取得 Database Reference (與 FirebaseManager 一致)
        // Path: game_sessions/{sessionID}/answers
        FirebaseDatabase.GetInstance(dbUrl).RootReference
            .Child("game_sessions")
            .Child(sessionID)
            .Child("answers")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogWarning($"[ResultManager] Firebase 載入失敗: {task.Exception}");
                    // 失敗時不動作，維持本機資料顯示
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists && snapshot.ChildrenCount > 0)
                    {
                        Debug.Log($"[ResultManager] 從 Firebase 載入 {snapshot.ChildrenCount} 筆答題紀錄");
                        ProcessFirebaseResults(snapshot);
                    }
                    else
                    {
                        Debug.Log("[ResultManager] Firebase 查無答題紀錄 (使用本機資料)");
                    }
                }
            });
    }

    /// <summary>
    /// [New] 處理 Firebase 回傳資料並更新圖表
    /// </summary>
    private void ProcessFirebaseResults(DataSnapshot snapshot)
    {
        // 1. 初始化統計容器：以 GlobalVariables.studentNames 為基準
        // Key: 學生名稱, Value: 答對題數
        Dictionary<string, int> firebaseScores = new Dictionary<string, int>();

        // 預先填入目前名單 (確保順序與顯示一致)
        if (GlobalVariables.studentNames != null)
        {
            foreach (var name in GlobalVariables.studentNames)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    if (!firebaseScores.ContainsKey(name))
                        firebaseScores.Add(name, 0);
                }
            }
        }
        else
        {
            return; // 沒名單無法統計
        }

        // 2. 遍歷 Firebase 資料
        foreach (DataSnapshot child in snapshot.Children)
        {
            // 嘗試解析 (注意：這裡假設 Firebase 欄位名稱正確)
            string studentName = child.Child("student").Value as string;
            object isCorrectObj = child.Child("is_correct").Value;
            
            bool isCorrect = false;
            if (isCorrectObj is bool bVal) isCorrect = bVal;
            else if (isCorrectObj is string sVal) bool.TryParse(sVal, out isCorrect);

            // 只統計名單內的學生
            if (!string.IsNullOrEmpty(studentName) && firebaseScores.ContainsKey(studentName))
            {
                if (isCorrect)
                {
                    firebaseScores[studentName]++;
                }
            }
            else
            {
                // 可選：紀錄非名單內的數據 (Debug用)
                // Debug.Log($"[ResultManager] 忽略未知學生或空名稱: {studentName}");
            }
        }

        // 3. 更新全域變數 (Optional: 若希望同步回本地存檔可更新，這裡僅更新顯示)
        // 為了讓 UpdateChartData 能直接用，我們暫時覆寫 GlobalVariables.playerCorrectCounts
        // (注意：這會改變本地狀態，符合「以 Firebase 為準」的邏輯)
        for (int i = 0; i < 4; i++)
        {
             if (i < GlobalVariables.studentNames.Length)
             {
                 string name = GlobalVariables.studentNames[i];
                 if (!string.IsNullOrEmpty(name) && firebaseScores.ContainsKey(name))
                 {
                     GlobalVariables.playerCorrectCounts[i] = firebaseScores[name];
                 }
                 else
                 {
                     GlobalVariables.playerCorrectCounts[i] = 0;
                 }
             }
        }

        // 4. 重繪圖表
        UpdateChartData();
        Debug.Log("[ResultManager]圖表已依據 Firebase 資料更新完成");
    }

    void UpdateChartData()
    {
        if (barChart == null) 
        {
            Debug.LogError("[ResultManager] BarChart 未綁定！請在 Inspector 拖入。");
            return;
        }

        // --- 1. 清除舊資料 ---
        var yAxis = barChart.GetChartComponent<YAxis>();
        if (yAxis != null)
        {
            yAxis.ClearData();
        }
        
        // 清空第一組數據 (Serie 0)
        Serie serie = null;
        if (barChart.series.Count > 0)
        {
            serie = barChart.series[0];
            serie.ClearData();
        }
        else
        {
            // 若沒有 Series，可考慮 AddSerie (目前假設場景已設定好)
            Debug.LogWarning("[ResultManager] BarChart 沒有 Series，無法顯示數據");
            return; 
        }

        // --- 2. 填入真實玩家數據 ---
        // 確保陣列不為空
        if (GlobalVariables.studentNames == null || GlobalVariables.playerCorrectCounts == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            // 安全檢查
            if (i >= GlobalVariables.studentNames.Length) break;
            if (i >= GlobalVariables.playerCorrectCounts.Length) break;

            string pName = GlobalVariables.studentNames[i];
            int correctCount = GlobalVariables.playerCorrectCounts[i];

            // 名字為空代表沒這個玩家，跳過
            if (string.IsNullOrEmpty(pName)) continue;

            // (A) 加入 Y 軸標籤 (玩家名字) -> 橫向圖表的 Category 在 Y 軸
            if (yAxis != null) yAxis.AddData(pName);

            // (B) 加入數值 (答對題數)
            if (serie != null)
            {
                serie.AddData(correctCount);
            }
        }

        // --- 3. 刷新圖表 ---
        barChart.RefreshChart();
    }

    void OnConfirmClicked()
    {
        // 顯示確認對話框
        if (confirmDialog != null)
        {
            confirmDialog.Show(
                "確定要登出並回到大廳？\n（資料將清空）",
                () => 
                {
                    // 確認後執行：清除狀態 -> 回大廳
                    GlobalVariables.ClearState();
                    SceneManager.LoadScene("LobbyScene");
                }
            );
        }
        else
        {
            // Fallback: 直接清除
            GlobalVariables.ClearState();
            SceneManager.LoadScene("LobbyScene");
        }
    }
}
