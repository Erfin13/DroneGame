using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// 大廳管理器 (LobbyManager)
/// [已整理: 增加欄位檢查、優化流程註解、確保異步處理安全]
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("UI 元件")]
    public TMP_InputField[] nameInputs; // 學生名字輸入框

    [Tooltip("建立場次按鈕")]
    public Button createSessionButton;

    public TextMeshProUGUI statusText;

    [Tooltip("相機按鈕（建立場次成功後才可按）")]
    public Button cameraButton;

    [Header("相機確認 Panel")]
    public CameraConfirmPanel cameraConfirmPanel; 

    // Private Fields
    private DatabaseReference dbReference;

    void Start()
    {
        Debug.Log("[LobbyManager] Start called");
        
        // 1. 初始化 Firebase DB Reference
        try 
        {
            // 使用全域設定的 URL
            string dbUrl = GlobalVariables.DATABASE_URL;
            dbReference = FirebaseDatabase.GetInstance(dbUrl).RootReference;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LobbyManager] Firebase Init Error: {e.Message}");
            if (statusText != null) statusText.text = "連線初始化失敗，請檢查網路";
        }

        // 2. 綁定按鈕事件
        if (createSessionButton != null)
        {
            createSessionButton.onClick.RemoveAllListeners(); // 防止重複綁定
            createSessionButton.onClick.AddListener(OnCreateSession);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] createSessionButton 未綁定");
        }

        if (cameraButton != null)
        {
            cameraButton.onClick.RemoveAllListeners();
            cameraButton.onClick.AddListener(OnCameraClicked);
            // 預設不可用，直到建立場次或檢測到已登入
            cameraButton.interactable = false; 
        }

        // 3. 檢查目前狀態 (是否從選角頁面返回)
        RestoreStateIfLoggedIn();
    }

    /// <summary>
    /// 若已存在 SessionID，則恢復 UI 狀態
    /// </summary>
    private void RestoreStateIfLoggedIn()
    {
        if (!string.IsNullOrEmpty(GlobalVariables.currentSessionID))
        {
            Debug.Log($"[LobbyManager] 偵測到已有 Session ID: {GlobalVariables.currentSessionID}，恢復 UI 狀態");
            
            if (statusText != null) statusText.text = "已建立場次，可以開始玩桌遊";
            if (createSessionButton != null) createSessionButton.interactable = false; // 已建立過，鎖定
            if (cameraButton != null) cameraButton.interactable = true; // 開放使用相機
            
            // 鎖定輸入框
            if (nameInputs != null)
            {
                foreach(var input in nameInputs)
                {
                    if(input != null) input.interactable = false;
                }
            }
        }
        else
        {
            // 尚未登入
            if (statusText != null)
                statusText.text = "請輸入名字，按「建立場次」後開始玩實體桌遊";
        }
    }

    void OnCreateSession()
    {
        // 清除本地舊資料
        if (GlobalVariables.localAnswerHistory != null)
            GlobalVariables.localAnswerHistory.Clear();
        else
            GlobalVariables.localAnswerHistory = new List<AnswerData>();

        // 讀取輸入框
        if (GlobalVariables.studentNames == null)
            GlobalVariables.studentNames = new string[4];

        if (nameInputs != null)
        {
            for (int i = 0; i < nameInputs.Length; i++)
            {
                // 若輸入框比陣列少，防止溢位
                if (i >= GlobalVariables.studentNames.Length) break;
                if (nameInputs[i] == null) continue;

                string inputName = nameInputs[i].text;
                // 預設名稱避免空白
                GlobalVariables.studentNames[i] = string.IsNullOrEmpty(inputName) ? $"學生_{i + 1}" : inputName;
            }
        }

        UploadSessionData();
    }

    void UploadSessionData()
    {
        if (statusText != null) statusText.text = "建立場次中...";
        if (createSessionButton != null) createSessionButton.interactable = false;
        if (cameraButton != null) cameraButton.interactable = false;

        // 準備上傳資料
        SessionData newSession = new SessionData
        {
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            students = new List<string>(GlobalVariables.studentNames)
        };

        if (dbReference == null)
        {
             Debug.LogError("[LobbyManager] dbReference 為 null，無法上傳");
             return;
        }

        string json = JsonUtility.ToJson(newSession);
        string key = dbReference.Child("game_sessions").Push().Key;

        // 設定 Session ID
        GlobalVariables.currentSessionID = key;

        // 非同步寫入 Firebase
        dbReference.Child("game_sessions").Child(key).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("上傳失敗: " + task.Exception);
                    if (statusText != null) statusText.text = "建立失敗，請重試";
                    if (createSessionButton != null) createSessionButton.interactable = true;
                    return;
                }

                Debug.Log($"[LobbyManager] 建立成功！Session ID: {key}");
                
                // 成功後流程：跳轉到 SelectPlayerScene
                Debug.Log("跳轉至 SelectPlayerScene...");
                
                // [關鍵] 儲存狀態，防止 Scene 切換後資料遺失
                GlobalVariables.SaveState(); 

                SceneManager.LoadScene("SelectPlayerScene");
            });
    }

    void OnCameraClicked()
    {
        // 雙重防護：需先建立場次
        if (string.IsNullOrEmpty(GlobalVariables.currentSessionID))
        {
             if (statusText != null) statusText.text = "請先建立場次，再進行掃描。";
             return;
        }

        if (cameraConfirmPanel != null)
        {
            cameraConfirmPanel.Show();
        }
        else
        {
            // 若 Panel 遺失，Fallback 直接跳轉
            Debug.LogWarning("[LobbyManager] CameraConfirmPanel 未綁定，直接跳轉 ScanScene...");
            SceneManager.LoadScene("ScanScene");
        }
    }
}
