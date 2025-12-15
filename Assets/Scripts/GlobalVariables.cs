using System.Collections.Generic;

/// <summary>
/// 全域變數管理
/// 儲存跨場景共用的資料，如學生名單、當前場次 ID 與答題紀錄
/// </summary>
public static class GlobalVariables
{
    #region Public Fields

    public static string[] studentNames = new string[4];
    public static string currentSessionID;
    public const string DATABASE_URL = "https://quizflow-analytics-default-rtdb.asia-southeast1.firebasedatabase.app/";

    /// <summary>
    /// 暫存所有答題紀錄 (給結算圖表用)
    /// </summary>
    public static List<AnswerData> localAnswerHistory = new List<AnswerData>();

    #endregion

    #region Public Methods

    /// <summary>
    /// 重置遊戲資料 (每次回大廳呼叫)
    /// </summary>
    public static void ResetGameData()
    {
        studentNames = new string[4];
        currentSessionID = "";
        localAnswerHistory.Clear();
    }

    #endregion
}