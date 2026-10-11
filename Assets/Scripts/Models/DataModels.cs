using System.Collections.Generic;

// 建議 3：將資料類別獨立出來，不再散落在各個檔案中

/// <summary>
/// 遊戲場次資料
/// 用於記錄單次遊戲的開始時間與學生名單
/// </summary>
[System.Serializable]
public class SessionData
{
    public string timestamp;
    public List<string> students;
}

/// <summary>
/// 答題紀錄資料
/// 用於記錄單一題目的回答情形，包含是否答對與耗時
/// </summary>
[System.Serializable]
public class AnswerData
{
    public string question_id;
    public string student;
    public bool is_correct;
    public float time_taken;
    public string timestamp;
}