using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

/// <summary>
/// 大廳管理器
/// 負責處理學生名字輸入與遊戲場次建立 (Session)
/// </summary>
public class LobbyManager : MonoBehaviour
{
    #region Fields

    [Header("UI 元件")]
    public TMP_InputField[] nameInputs;
    public Button startButton;
    public TextMeshProUGUI statusText;

    private DatabaseReference dbReference;

    #endregion

    #region Unity Methods

    /// <summary>
    /// 初始化 Firebase 參考並綁定開始按鈕事件
    /// </summary>
    void Start()
    {
        // 使用 GlobalVariables 裡的網址
        string dbUrl = GlobalVariables.DATABASE_URL;
        dbReference = FirebaseDatabase.GetInstance(dbUrl).RootReference;

        startButton.onClick.AddListener(OnStartGame);
        
        if(statusText != null) statusText.text = "請輸入名字後開始";
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 開始遊戲按鈕事件
    /// 蒐集輸入的名字並呼叫上傳
    /// </summary>
    void OnStartGame()
    {
        GlobalVariables.localAnswerHistory.Clear();
        for (int i = 0; i < nameInputs.Length; i++)
        {
            string inputName = nameInputs[i].text;
            if (string.IsNullOrEmpty(inputName))
            {
                GlobalVariables.studentNames[i] = $"學生_{i + 1}";
            }
            else
            {
                GlobalVariables.studentNames[i] = inputName;
            }
        }
        UploadSessionData();
    }

    /// <summary>
    /// 上傳場次資料 (SessionData) 到 Firebase Database
    /// 上傳成功後自動切換至 GameScene
    /// </summary>
    void UploadSessionData()
    {
        if(statusText != null) statusText.text = "資料上傳中...";
        startButton.interactable = false;

        // SessionData 現在是從 DataModels.cs 來的
        SessionData newSession = new SessionData();
        newSession.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        newSession.students = new List<string>(GlobalVariables.studentNames);

        string json = JsonUtility.ToJson(newSession);
        string key = dbReference.Child("game_sessions").Push().Key;
        GlobalVariables.currentSessionID = key;

        dbReference.Child("game_sessions").Child(key).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("上傳失敗: " + task.Exception);
                if(statusText != null) statusText.text = "上傳失敗，請檢查網路";
                startButton.interactable = true;
            }
            else
            {
                Debug.Log("名單上傳成功！ID: " + key);
                SceneManager.LoadScene("GameScene");
            }
        });
    }

    #endregion
}