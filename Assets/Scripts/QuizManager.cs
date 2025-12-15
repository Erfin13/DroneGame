using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

/// <summary>
/// 測驗管理器
/// 負責處理題目載入、顯示、答題邏輯以及分數上傳
/// </summary>
public class QuizManager : MonoBehaviour
{
    #region Fields

    [Header("UI 元件")]
    public TextMeshProUGUI questionTextUI;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI currentPlayerText;

    [Header("音效設定")]
    public AudioSource sfxSource;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    [Header("顏色設定")]
    public Color normalColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    private List<Question> allQuestions = new List<Question>();
    private int currentQuestionIndex = 0;
    private float questionStartTime;
    private DatabaseReference dbReference;
    private bool isAnswering = false;
    private int currentPlayerIndex = 0;

    #endregion

    #region Unity Methods

    /// <summary>
    /// 初始化 Firebase 參考並開始下載題目
    /// </summary>
    void Start()
    {
        if (loadingText != null) loadingText.gameObject.SetActive(true);
        if (questionTextUI != null) questionTextUI.gameObject.SetActive(false);
        if (currentPlayerText != null) currentPlayerText.text = "";

        if (GlobalVariables.studentNames[0] == null)
        {
            GlobalVariables.studentNames = new string[] { "測試A", "測試B", "測試C", "測試D" };
        }

        // 使用 GlobalVariables 裡的網址
        string dbUrl = GlobalVariables.DATABASE_URL;
        dbReference = FirebaseDatabase.GetInstance(dbUrl).RootReference;

        LoadQuestionsFromFirebase();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 從 Firebase Realtime Database 下載題目資料
    /// </summary>
    void LoadQuestionsFromFirebase()
    {
        Debug.Log("開始下載題目...");
        dbReference.Child("questions").GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("下載失敗: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                allQuestions.Clear();

                foreach (DataSnapshot questionNode in snapshot.Children)
                {
                    Question q = new Question();
                    q.id = questionNode.Key;
                    q.questionText = questionNode.Child("questionText").Value != null ? questionNode.Child("questionText").Value.ToString() : "Error Text";
                    q.correctOptionIndex = questionNode.Child("correctOptionIndex").Value != null ? int.Parse(questionNode.Child("correctOptionIndex").Value.ToString()) : 0;

                    if (questionNode.HasChild("difficultyLevel"))
                        q.difficultyLevel = int.Parse(questionNode.Child("difficultyLevel").Value.ToString());
                    else
                        q.difficultyLevel = 1;

                    q.options = new List<string>();
                    foreach (DataSnapshot opt in questionNode.Child("options").Children)
                    {
                        q.options.Add(opt.Value.ToString());
                    }
                    allQuestions.Add(q);
                }
                Debug.Log($"成功下載 {allQuestions.Count} 題！");

                if (loadingText != null) loadingText.gameObject.SetActive(false);
                if (questionTextUI != null) questionTextUI.gameObject.SetActive(true);
                ShowQuestion();
            }
        });
    }

    /// <summary>
    /// 顯示目前的題目與選項，並切換當前答題者
    /// </summary>
    void ShowQuestion()
    {
        if (currentQuestionIndex >= allQuestions.Count)
        {
            if (questionTextUI != null) questionTextUI.text = "測驗結束！";
            Invoke("GoToResultScene", 2.0f);
            if (currentPlayerText != null) currentPlayerText.text = "";
            return;
        }

        // 輪流答題邏輯：使用餘數運算確保在學生名單中循環
        // 比如 4 個學生，第 5 題 (Index 4) 輪回第 0 位學生
        int totalStudents = GlobalVariables.studentNames.Length;
        currentPlayerIndex = currentQuestionIndex % totalStudents;
        
        string currentName = GlobalVariables.studentNames[currentPlayerIndex];

        if (currentPlayerText != null)
        {
            currentPlayerText.text = $"現在答題：{currentName}";
        }

        Question currentQ = allQuestions[currentQuestionIndex];
        questionTextUI.text = currentQ.questionText;

        isAnswering = true;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].image.color = normalColor;
            optionButtons[i].interactable = true;

            if (i < currentQ.options.Count)
            {
                optionTexts[i].text = currentQ.options[i];
                optionButtons[i].gameObject.SetActive(true);
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }

            optionButtons[i].onClick.RemoveAllListeners();
            int index = i;
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }

        questionStartTime = Time.time;
    }

    /// <summary>
    /// 切換至結算場景
    /// </summary>
    void GoToResultScene()
    {
        SceneManager.LoadScene("ResultScene");
    }

    /// <summary>
    /// 處理選項按鈕點擊事件，判定對錯並上傳資料
    /// </summary>
    /// <param name="selectedIndex">玩家選擇的選項 Index</param>
    void OnOptionSelected(int selectedIndex)
    {
        if (!isAnswering) return;
        isAnswering = false;

        Question currentQ = allQuestions[currentQuestionIndex];
        float timeTaken = Time.time - questionStartTime;
        bool isCorrect = (selectedIndex == currentQ.correctOptionIndex);

        Image btnImage = optionButtons[selectedIndex].image;
        if (isCorrect)
        {
            btnImage.color = correctColor;
            if (sfxSource != null && correctClip != null) sfxSource.PlayOneShot(correctClip);
        }
        else
        {
            btnImage.color = wrongColor;
            if (sfxSource != null && wrongClip != null) sfxSource.PlayOneShot(wrongClip);
        }

        foreach (var btn in optionButtons) btn.interactable = false;

        string currentStudentID = GlobalVariables.studentNames[currentPlayerIndex];

        // 1. 上傳 Analytics (Firebase 分析)
        // 記錄該次答題的詳細資訊供後台分析
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.LogAnswer(currentQ.id, isCorrect, timeTaken, currentStudentID);
        }

        // 2. 上傳到 Database
        // 透過 FirebaseManager 將答題紀錄寫入即時資料庫
        if (!string.IsNullOrEmpty(GlobalVariables.currentSessionID) && FirebaseManager.Instance != null)
        {
            AnswerData data = new AnswerData();
            data.question_id = currentQ.id;
            data.student = currentStudentID;
            data.is_correct = isCorrect;
            data.time_taken = timeTaken;
            data.timestamp = System.DateTime.Now.ToString("HH:mm:ss");

            FirebaseManager.Instance.UploadAnswerToDB(GlobalVariables.currentSessionID, data);
            
            // 資料暫存：將答題紀錄存入本地列表，以便在結算畫面顯示統計
            GlobalVariables.localAnswerHistory.Add(data);
        }

        StartCoroutine(NextQuestionRoutine());
    }

    /// <summary>
    /// 延遲後進入下一題
    /// </summary>
    IEnumerator NextQuestionRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        currentQuestionIndex++;
        ShowQuestion();
    }

    #endregion
}