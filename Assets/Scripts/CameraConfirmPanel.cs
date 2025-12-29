using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 相機確認面板
/// 顯示「是否開啟相機掃描？」的確認框
/// </summary>
public class CameraConfirmPanel : MonoBehaviour
{
    [Header("Panel Root (把自己拖進去)")]
    public GameObject panelRoot;

    [Header("Buttons")]
    public Button confirmButton;
    public Button cancelButton;

    void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        
        // 預設關閉
        panelRoot.SetActive(false);

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
    }

    public void Show()
    {
        panelRoot.SetActive(true);
    }

    void OnCancel()
    {
        panelRoot.SetActive(false);
    }

    void OnConfirm()
    {
        // 隱藏面板
        panelRoot.SetActive(false);

        // 跳轉到 ScanScene
        Debug.Log("[CameraConfirmPanel] Confirmed. Loading ScanScene...");
        SceneManager.LoadScene("ScanScene");
    }
}
