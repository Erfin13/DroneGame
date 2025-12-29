using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

/// <summary>
/// 測驗管理器
/// 負責處理題目載入、顯示、答題邏輯以及分數上傳
/// [已整理: 題目載入防呆、答題連點防護、Firebase路徑確保]
/// </summary>
public class QuizManager : MonoBehaviour
{
    #region Fields

    [Header("UI 元件")]
    public TextMeshProUGUI questionTextUI;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI rewardTextUI; // 新增：顯示本題能量

    [Header("音效設定")]
    public AudioSource sfxSource;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    [Header("顏色設定")]
    public Color normalColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    private List<Question> allQuestions = new List<Question>();
    private float questionStartTime;
    private DatabaseReference dbReference;
    private bool isAnswering = false;

    #endregion

    #region Unity Methods

    void Start()
    {
        // UI 初始化
        if (loadingText != null) loadingText.gameObject.SetActive(true);
        if (questionTextUI != null) questionTextUI.gameObject.SetActive(false);
        if (rewardTextUI != null) rewardTextUI.text = "";

        // 安全檢查: 玩家與 Session
        GlobalVariables.LoadState(); // Double Ensure
        if (GlobalVariables.currentPlayerIndex < 0) 
        {
             Debug.LogWarning("[QuizManager] 未選擇玩家，索引重置為 0");
             GlobalVariables.currentPlayerIndex = 0;
        }

        // Firebase Init
        try 
        {
            string dbUrl = GlobalVariables.DATABASE_URL;
            dbReference = FirebaseDatabase.GetInstance(dbUrl).RootReference;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[QuizManager] Firebase Init Failed: {e.Message}");
            if (loadingText != null) loadingText.text = "連線失敗";
            return;
        }

        LoadQuestionsFromFirebase();
    }

    #endregion

    #region Private Methods

    void LoadQuestionsFromFirebase()
    {
        allQuestions.Clear();
        string targetQid = GlobalVariables.selectedQuestionId;

        if (!string.IsNullOrEmpty(targetQid))
        {
            Debug.Log($"[QuizManager] 載入單題模式 ID: {targetQid}");
            dbReference.Child("questions").Child(targetQid).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("[QuizManager] 題目載入失敗");
                    HandleLoadError("載入錯誤，請重試");
                    return;
                }

                if (task.IsCompleted && task.Result.Exists)
                {
                    DataSnapshot snapshot = task.Result;
                    Question q = ParseQuestionSnapshot(snapshot);
                    allQuestions.Add(q);
                    
                    GlobalVariables.selectedQuestionId = ""; // 用完清空，避免重複
                    OnQuestionsLoaded();
                }
                else
                {
                    HandleLoadError("題目不存在或 QR Code 無效");
                }
            });
        }
        else
        {
            // 正常流程不應發生，除非直接 Play Scene
            HandleLoadError("未掃描任何題目 (QID Empty)");
        }
    }

    void HandleLoadError(string msg)
    {
        if (loadingText != null) loadingText.text = msg;
        GlobalVariables.selectedQuestionId = ""; 
        Invoke("BackToSelectPlayer", 2.0f);
    }

    void BackToSelectPlayer()
    {
        SceneManager.LoadScene("SelectPlayerScene");
    }

    Question ParseQuestionSnapshot(DataSnapshot questionNode)
    {
        Question q = new Question();
        q.id = questionNode.Key;

        // 安全解析各欄位
        q.type = questionNode.HasChild("type") ? questionNode.Child("type").Value.ToString() : "quiz";
        
        if (questionNode.HasChild("reward"))
            int.TryParse(questionNode.Child("reward").Value.ToString(), out q.reward);
        else 
            q.reward = 10;

        q.questionText = questionNode.Child("questionText").Value != null ? questionNode.Child("questionText").Value.ToString() : "";
        
        if (questionNode.Child("correctOptionIndex").Value != null)
            int.TryParse(questionNode.Child("correctOptionIndex").Value.ToString(), out q.correctOptionIndex);
        else
            q.correctOptionIndex = 0;

        q.options = new List<string>();
        if (questionNode.HasChild("options"))
        {
            foreach (DataSnapshot opt in questionNode.Child("options").Children)
                q.options.Add(opt.Value.ToString());
        }
        return q;
    }

    void OnQuestionsLoaded()
    {
        if (loadingText != null) loadingText.gameObject.SetActive(false);
        if (questionTextUI != null) questionTextUI.gameObject.SetActive(true);
        
        if (allQuestions.Count > 0) ShowQuestion();
    }

    void ShowQuestion()
    {
        // 只顯示第一題 (單題模式)
        if (allQuestions.Count == 0) return;
        Question currentQ = allQuestions[0];
        
        if (questionTextUI != null) questionTextUI.text = currentQ.questionText;

        // 顯示能量獎勵
        if (rewardTextUI != null)
        {
            rewardTextUI.text = $"本題答對可獲得 {currentQ.reward} 能量";
        }

        isAnswering = true;

        if (optionButtons != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null) continue;

                optionButtons[i].image.color = normalColor;
                optionButtons[i].interactable = true;

                // 設定選項文字
                if (currentQ.options != null && i < currentQ.options.Count)
                {
                    if (optionTexts[i] != null) optionTexts[i].text = currentQ.options[i];
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
        }

        questionStartTime = Time.time;
    }

    void OnOptionSelected(int selectedIndex)
    {
        if (!isAnswering) return;
        isAnswering = false;

        Question currentQ = allQuestions[0];
        float timeTaken = Time.time - questionStartTime;
        bool isCorrect = (selectedIndex == currentQ.correctOptionIndex);

        // UI 回饋
        if (optionButtons != null && selectedIndex < optionButtons.Length)
        {
            Image btnImage = optionButtons[selectedIndex].image;
            if (isCorrect)
            {
                btnImage.color = correctColor;
                if (sfxSource != null && correctClip != null) sfxSource.PlayOneShot(correctClip);
                
                // 更新計分
                if (GlobalVariables.currentPlayerIndex >= 0 && GlobalVariables.currentPlayerIndex < 4)
                {
                    GlobalVariables.playerCorrectCounts[GlobalVariables.currentPlayerIndex]++;
                    GlobalVariables.SaveState(); // 立即存檔
                }
            }
            else
            {
                btnImage.color = wrongColor;
                if (sfxSource != null && wrongClip != null) sfxSource.PlayOneShot(wrongClip);
            }
        }

        // 鎖定所有按鈕
        if (optionButtons != null)
        {
            foreach (var btn in optionButtons) 
                if (btn != null) btn.interactable = false;
        }

        // 上傳紀錄
        if (FirebaseManager.Instance != null && !string.IsNullOrEmpty(GlobalVariables.currentSessionID))
        {
            string currentStudentID = GlobalVariables.studentNames[GlobalVariables.currentPlayerIndex];
            AnswerData data = new AnswerData
            {
                question_id = currentQ.id,
                student = currentStudentID,
                is_correct = isCorrect,
                time_taken = timeTaken,
                timestamp = System.DateTime.Now.ToString("HH:mm:ss")
            };
            FirebaseManager.Instance.UploadAnswerToDB(GlobalVariables.currentSessionID, data);
            
            // Analytics
            FirebaseManager.Instance.LogAnswer(currentQ.id, isCorrect, timeTaken, currentStudentID);
        }

        // 答題結束後，一律回到 SelectPlayerScene (延遲 1.5 秒讓玩家看結果)
        Invoke("BackToSelectPlayer", 1.5f);
    }

    #endregion
}
