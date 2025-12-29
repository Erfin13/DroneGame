using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.Database;
using Firebase.Extensions; 

/// <summary>
/// Firebase 管理器
/// 負責初始化 Firebase 服務並處理 Analytics (分析) 與 Database (資料庫) 的上傳
/// [已整理: 單例保護、初始化狀態檢查]
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    #region Static Instance

    public static FirebaseManager Instance;

    #endregion

    #region Private Fields

    private bool isFirebaseInitialized = false;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        // 單例模式 (Singleton Pattern)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 若重複存在則刪除新的，保持唯一性
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 檢查並修復依賴 (Android 常見需求)
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Firebase 初始化失敗，狀態: " + dependencyStatus);
            }
        });
    }

    #endregion

    #region Private Methods

    private void InitializeFirebase()
    {
        isFirebaseInitialized = true;
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        // 設定 Session Timeout (Optional, 視需求而定)
        FirebaseAnalytics.SetSessionTimeoutDuration(new System.TimeSpan(0, 30, 0));
        Debug.Log("Firebase Analytics 準備完成！");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 上傳答題事件至 Firebase Analytics
    /// </summary>
    public void LogAnswer(string questionId, bool isCorrect, float timeTaken, string studentID)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogWarning("[FirebaseManager] 尚未初始化，無法記錄 Analytics");
            return;
        }

        Parameter[] parameters = {
            new Parameter("question_id", questionId ?? "unknown"),
            new Parameter("result", isCorrect ? "correct" : "wrong"),
            new Parameter("response_time", timeTaken),
            new Parameter("student_id", studentID ?? "unknown"),
            new Parameter("timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
        };

        FirebaseAnalytics.LogEvent("question_answered", parameters);
        Debug.Log($"[Analytics] 學生:{studentID}, 題號:{questionId}, 結果:{isCorrect}");
    }

    /// <summary>
    /// 上傳答題紀錄至 Firebase Realtime Database
    /// </summary>
    /// <param name="sessionID">遊戲場次 ID</param>
    /// <param name="data">答題資料物件</param>
    public void UploadAnswerToDB(string sessionID, AnswerData data)
    {
        if (string.IsNullOrEmpty(sessionID))
        {
            Debug.LogWarning("[FirebaseManager] SessionID 為空，無法上傳 Database");
            return;
        }

        // 確保使用 GlobalVariables 的 URL
        string dbUrl = GlobalVariables.DATABASE_URL;
        string json = JsonUtility.ToJson(data);

        // 使用 RootReference 以確保路徑一致
        // Path: game_sessions/{sessionID}/answers/{pushID}
        FirebaseDatabase.GetInstance(dbUrl).RootReference
            .Child("game_sessions")
            .Child(sessionID)
            .Child("answers")
            .Push()
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task => 
            {
                if (task.IsFaulted)
                    Debug.LogError("[Database] 上傳失敗: " + task.Exception);
                else
                    Debug.Log("[Database] 答題紀錄已上傳");
            });
    }

    #endregion
}