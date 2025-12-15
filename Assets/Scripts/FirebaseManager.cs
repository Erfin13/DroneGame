using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.Database;
using Firebase.Extensions; 

/// <summary>
/// Firebase 管理器
/// 負責初始化 Firebase 服務並處理 Analytics (分析) 與 Database (資料庫) 的上傳
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

    /// <summary>
    /// 單例模式初始化
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 檢查 Firebase 依賴並初始化
    /// </summary>
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Firebase 初始化失敗: " + dependencyStatus);
            }
        });
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 初始化 Firebase 參數並啟用 Analytics
    /// </summary>
    void InitializeFirebase()
    {
        isFirebaseInitialized = true;
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        Debug.Log("Firebase Analytics 準備完成！");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 上傳答題事件至 Firebase Analytics
    /// </summary>
    public void LogAnswer(string questionId, bool isCorrect, float timeTaken, string studentID)
    {
        if (!isFirebaseInitialized) return;

        Parameter[] parameters = {
            new Parameter("question_id", questionId),
            new Parameter("result", isCorrect ? "correct" : "wrong"),
            new Parameter("response_time", timeTaken),
            new Parameter("student_id", studentID),
            new Parameter("timestamp", System.DateTime.Now.ToString())
        };

        FirebaseAnalytics.LogEvent("question_answered", parameters);
        Debug.Log($"[Analytics] 學生:{studentID}, 題號:{questionId}");
    }

    /// <summary>
    /// 上傳答題紀錄至 Firebase Realtime Database
    /// </summary>
    /// <param name="sessionID">遊戲場次 ID</param>
    /// <param name="data">答題資料物件</param>
    public void UploadAnswerToDB(string sessionID, AnswerData data)
    {
        // 直接使用 GlobalVariables 裡的網址，不用傳參數進來
        string json = JsonUtility.ToJson(data);
        
        FirebaseDatabase.GetInstance(GlobalVariables.DATABASE_URL).RootReference
            .Child("game_sessions")
            .Child(sessionID)
            .Child("answers")
            .Push()
            .SetRawJsonValueAsync(json);
            
        Debug.Log("[Database] 答題紀錄已上傳");
    }

    #endregion
}