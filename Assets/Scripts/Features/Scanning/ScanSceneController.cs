using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ScanScene 的主要控制器
/// [已修正: 寬鬆檢查 QR 格式，避免因掃到非標準碼而導致相機卡住; 解析成功才手動停止相機]
/// </summary>
public class ScanSceneController : MonoBehaviour
{
    [Header("核心元件")]
    public QRScanManager scanManager;

    [Header("UI 元件")]
    public Button backButton;

    [Header("場景設定")]
    public string nextSceneName = "GameScene";

    // 避免重複處裡
    private bool isProcessing = false;

    void Start()
    {
        if (scanManager == null) scanManager = FindObjectOfType<QRScanManager>();

        if (scanManager != null)
        {
            Debug.Log("[ScanSceneController] 啟動掃描...");
            isProcessing = false;
            // ScanManager 現在不會自動停止，而是持續回報直到我們滿意
            scanManager.StartScan(OnScanResult);
        }
        else
        {
            Debug.LogError("[ScanSceneController] 找不到 QRScanManager");
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    void OnBackClicked()
    {
        Debug.Log("[ScanSceneController] 返回大廳...");
        StopCameraSafe();
        SceneManager.LoadScene("LobbyScene");
    }

    void OnScanResult(string result)
    {
        if (isProcessing) return;

        // 這裡不要 log 每一幀，因為 Manager 沒停止前可能會一直觸發
        // 但為了除錯先印出
        // Debug.Log($"[ScanSceneController] 收到 QR: {result}");

        // 1. 寬鬆檢查：只要包含 "qid=" 就嘗試解析，不強制 "QUIZ|" 開頭
        // 這樣可避免舊版 QR Code 或格式微調後掃不到
        if (string.IsNullOrEmpty(result) || !result.Contains("qid="))
        {
            // 掃到無效碼 -> 忽略，讓相機繼續運作
            return;
        }

        // 2. 解析
        string qid = ParseQid(result);

        // 3. 嚴格檢查 QID
        // 只能是 qL1, qL2, qL3
        // 禁止 qL1_01 或其他任何格式
        int difficultyCandidate = 0;

        if (qid == "qL1") difficultyCandidate = 1;
        else if (qid == "qL2") difficultyCandidate = 2;
        else if (qid == "qL3") difficultyCandidate = 3;
        
        if (difficultyCandidate > 0)
        {
            Debug.Log($"[ScanSceneController] 解析成功 Diff: {difficultyCandidate}, 準備轉場...");
            isProcessing = true;

            // 設定難度
            GlobalVariables.selectedDifficultyLevel = difficultyCandidate;
            // 確保清除單題模式
            GlobalVariables.selectedQuestionId = "";

            // 成功後，手動呼叫停止
            StopCameraSafe();

            // 轉場
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // 若為 qL1_01 或其他格式，一律忽略
            Debug.LogWarning($"[ScanSceneController] 掃描到無效或被禁止的 QID: {qid} (僅支援 qL1/qL2/qL3)");
        }
    }

    void StopCameraSafe()
    {
        if (scanManager != null)
        {
            scanManager.StopScan();
        }
    }

    private string ParseQid(string raw)
    {
        try
        {
            // 簡易解析：尋找 "qid="
            // 支援 QUIZ|qid=q01 或 http://...?qid=q01 等格式
            string[] parts = raw.Split(new char[] { '|', '&', '?' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string p in parts)
            {
                if (p.StartsWith("qid="))
                {
                    return p.Substring(4);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ScanSceneController] 解析例外: {ex.Message}");
        }
        return "";
    }
}
