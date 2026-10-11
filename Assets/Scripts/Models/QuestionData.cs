using UnityEngine;

/// <summary>
/// 題目資料 ScriptableObject
/// 允許在 Unity Editor 中建立題目資產
/// </summary>
[CreateAssetMenu(fileName = "NewQuestion", menuName = "Quiz/Question Data")]
public class QuestionData : ScriptableObject
{
    [Header("題目設定")]
    [Tooltip("題目ID (例如: Q001)，分析數據時用")]
    public string questionID;       

    [TextArea]
    public string questionText;

    [Header("選項設定 (請填4個)")]
    public string[] options;        

    [Header("答案設定")]
    [Tooltip("0代表第一個選項, 1代表第二個...")]
    public int correctOptionIndex;  

    [Header("難度分析用")]
    [Tooltip("1=簡單, 2=中等, 3=困難")]
    public int difficultyLevel;     
}