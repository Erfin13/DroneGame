using System.Collections.Generic;

/// <summary>
/// 題目資料結構 (Runtime)
/// 用於遊戲執行時儲存題目資訊，對應 Firebase 資料結構
/// </summary>
[System.Serializable]
public class Question
{
    public string id; 
    
    /// <summary>
    /// 類型: "quiz" (一般問答) 或 "supply" (補給站)
    /// </summary>
    public string type;

    /// <summary>
    /// 獲得能量 (答對或補給成功)
    /// </summary>
    public int reward;

    public string questionText;
    public List<string> options;
    public int correctOptionIndex;
    public int difficultyLevel;
}