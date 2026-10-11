using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 選擇回答對象場景管理器
/// [已整理: 登入檢查 Coroutine 優化、按鈕事件優化]
/// </summary>
public class SelectPlayerManager : MonoBehaviour
{
    [Header("UI 元件")]
    [Tooltip("4 個玩家按鈕")]
    public Button[] playerButtons;
    
    [Tooltip("對應按鈕上的文字 (顯示玩家名)")]
    public TextMeshProUGUI[] playerButtonTexts;

    [Tooltip("登出按鈕 (前往結算)")]
    public Button logoutButton;

    [Tooltip("確認對話框 (需綁定)")]
    public ConfirmDialog confirmDialog;

    void Start()
    {
        // 確保先嘗試從 PlayerPrefs 載入狀態 (防止 Editor 直接 Play 資料為空)
        GlobalVariables.LoadState();

        // 為了避免剛切換場景時資料尚未同步，使用 Coroutine 延遲檢查
        StartCoroutine(CheckLoginStatusRoutine());
    }

    // 登入狀態檢查流程
    private System.Collections.IEnumerator CheckLoginStatusRoutine()
    {
        // 等待 0.2s 確保變數穩定
        yield return new WaitForSeconds(0.2f);

        // Double Check: 若為空則再讀一次
        if (string.IsNullOrEmpty(GlobalVariables.currentSessionID))
        {
            GlobalVariables.LoadState();
        }

        // 嚴格檢查 SessionID
        if (string.IsNullOrEmpty(GlobalVariables.currentSessionID)) 
        {
            Debug.LogWarning($"[SelectPlayerManager] 判定未登入 (SessionID Empty). 返回 Lobby.");
            SceneManager.LoadScene("LobbyScene");
            yield break; // 結束 Coroutine
        }

        Debug.Log($"[SelectPlayerManager] 登入驗證通過: {GlobalVariables.currentSessionID}");

        // 初始化 UI
        InitPlayerButtons();

        // 綁定登出
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }
    }

    void InitPlayerButtons()
    {
        if (playerButtons == null) return;

        for (int i = 0; i < playerButtons.Length; i++)
        {
            if (playerButtons[i] == null) continue;

            // 超出名單範圍或無名字則隱藏
            if (GlobalVariables.studentNames == null || 
                i >= GlobalVariables.studentNames.Length || 
                string.IsNullOrEmpty(GlobalVariables.studentNames[i]))
            {
                playerButtons[i].gameObject.SetActive(false);
                continue;
            }

            // 顯示並設定文字
            string pName = GlobalVariables.studentNames[i];
            playerButtons[i].gameObject.SetActive(true);
            
            if (i < playerButtonTexts.Length && playerButtonTexts[i] != null) 
                playerButtonTexts[i].text = pName;

            // 綁定點擊事件
            int index = i; // Closure capture
            playerButtons[i].onClick.RemoveAllListeners();
            playerButtons[i].onClick.AddListener(() => OnPlayerSelected(index));
        }
    }

    void OnPlayerSelected(int index)
    {
        string pName = GlobalVariables.studentNames[index];
        Debug.Log($"[SelectPlayerManager] 選擇玩家: {pName} (Index: {index})");
        
        // 設定並儲存當前玩家 Index
        GlobalVariables.currentPlayerIndex = index;
        GlobalVariables.SaveState(); // 重要：儲存狀態

        // 跳轉掃描
        SceneManager.LoadScene("ScanScene");
    }

    void OnLogoutClicked()
    {
        // 跳出確認視窗
        if (confirmDialog != null)
        {
            confirmDialog.Show(
                "確定要結束本局並查看結算嗎？",
                () => 
                {
                    Debug.Log("[SelectPlayerManager] 登出，前往結算...");
                    SceneManager.LoadScene("ResultScene");
                }
            );
        }
        else
        {
            // Fallback
            Debug.LogWarning("[SelectPlayerManager] ConfirmDialog 未綁定，直接跳轉");
            SceneManager.LoadScene("ResultScene");
        }
    }
}
