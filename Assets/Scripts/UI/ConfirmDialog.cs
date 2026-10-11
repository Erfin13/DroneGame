using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 通用確認對話視窗
/// 可在不同場景重複使用
/// </summary>
public class ConfirmDialog : MonoBehaviour
{
    [Header("UI 元件")]
    [Tooltip("Panel 的根物件 (用來開關顯示)")]
    public GameObject panelRoot;

    [Tooltip("顯示訊息的文字")]
    public TextMeshProUGUI messageText;

    [Tooltip("確認按鈕")]
    public Button confirmButton;

    [Tooltip("取消按鈕")]
    public Button cancelButton;

    private Action onConfirmCallback;
    private Action onCancelCallback;

    void Awake()
    {
        // 預設關閉
        if (panelRoot != null) panelRoot.SetActive(false);

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
    }

    /// <summary>
    /// 顯示確認視窗
    /// </summary>
    /// <param name="message">提示文字</param>
    /// <param name="onConfirm">按下確認執行的動作</param>
    /// <param name="onCancel">按下取消執行的動作 (可選)</param>
    public void Show(string message, Action onConfirm, Action onCancel = null)
    {
        if (messageText != null) messageText.text = message;
        
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void OnConfirmClicked()
    {
        Hide();
        onConfirmCallback?.Invoke();
    }

    void OnCancelClicked()
    {
        Hide();
        onCancelCallback?.Invoke();
    }
}
