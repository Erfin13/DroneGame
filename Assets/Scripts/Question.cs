using System.Collections.Generic;

/// <summary>
/// 題目資料結構 (Runtime)
/// 用於遊戲執行時儲存題目資訊
/// </summary>
[System.Serializable]
public class Question
{
    public string id; 
    public string questionText;
    public List<string> options;
    public int correctOptionIndex;
    public int difficultyLevel;
}