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
        
        // 嚴格禁止固定題號模式
        if (!string.IsNullOrEmpty(GlobalVariables.selectedQuestionId))
        {
            HandleLoadError("錯誤：禁止使用固定題號，請掃描 qL1/qL2/qL3");
            return;
        }

        int targetDiff = GlobalVariables.selectedDifficultyLevel;
        if (targetDiff < 1 || targetDiff > 3)
        {
            HandleLoadError("錯誤：未選擇有效難度 (請重新掃描 qL1/qL2/qL3)");
            return;
        }

        Debug.Log($"[QuizManager] 準備載入難度 {targetDiff} 的題目...");

        // 從 Root 抓取全部資料，再進行篩選
        dbReference.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[QuizManager] 題目載入失敗 (Network/Firebase Error)");
                HandleLoadError("資料庫連線錯誤");
                return;
            }

            if (task.IsCompleted && task.Result.Exists)
            {
                List<Question> candidates = new List<Question>();
                
                // 遍歷 Root 下所有 Children (假設每個 Child 是一題)
                foreach (DataSnapshot child in task.Result.Children)
                {
                    try
                    {
                        Question q = ParseQuestionSnapshot(child);
                        // 篩選難度
                        if (q.difficultyLevel == targetDiff)
                        {
                            candidates.Add(q);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // 略過解析失敗的節點 (可能不是題目)
                        // Debug.LogWarning($"[QuizManager] Node {child.Key} parse skip: {ex.Message}");
                    }
                }

                if (candidates.Count == 0)
                {
                    HandleLoadError($"題庫中沒有難度 {targetDiff} 的題目");
                    return;
                }

                // 隨機抽題邏輯
                if (!GlobalVariables.usedQuestionIdsByDifficulty.ContainsKey(targetDiff))
                {
                    GlobalVariables.usedQuestionIdsByDifficulty[targetDiff] = new HashSet<string>();
                }

                HashSet<string> usedSet = GlobalVariables.usedQuestionIdsByDifficulty[targetDiff];
                
                // 找出尚未出過的題目
                List<Question> available = new List<Question>();
                foreach (var q in candidates)
                {
                    if (!usedSet.Contains(q.id))
                    {
                        available.Add(q);
                    }
                }

                // 若全部都出過了，重置 (或是看需求是否要結束，這邊先重置)
                if (available.Count == 0)
                {
                    Debug.Log($"[QuizManager] 難度 {targetDiff} 題目已全數出完，重置紀錄...");
                    usedSet.Clear();
                    available.AddRange(candidates);
                }

                // 隨機選一題
                int rndIndex = Random.Range(0, available.Count);
                Question finalQ = available[rndIndex];

                // 標記為已使用
                usedSet.Add(finalQ.id);

                Debug.Log($"[QuizManager] 選中題目: {finalQ.id} (Diff: {finalQ.difficultyLevel})");

                allQuestions.Add(finalQ);

                // 清除暫存難度，避免重複進來
                GlobalVariables.selectedDifficultyLevel = 0;

                OnQuestionsLoaded();
            }
            else
            {
                HandleLoadError("資料庫是空的");
            }
        });
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

        // 解析難度 (新增)
        if (questionNode.HasChild("difficultyLevel"))
        {
            long diffVal = 1;
            // Firebase 數值通常為 long
            if (long.TryParse(questionNode.Child("difficultyLevel").Value.ToString(), out diffVal))
                q.difficultyLevel = (int)diffVal;
            else
                q.difficultyLevel = 1; // 預設
        }
        else
        {
            q.difficultyLevel = 1; // 若沒欄位預設為 1
        }

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
