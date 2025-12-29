using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using XCharts.Runtime; 

/// <summary>
/// 結算場景管理器 (XCharts 版本 - 含防誤觸)
/// [已整理: 資料載入防呆、UI 更新邏輯優化]
/// </summary>
public class ResultManager : MonoBehaviour
{
    [Header("XCharts 元件")]
    [Tooltip("請拖入場景中的 BarChart 物件")]
    public BarChart barChart;

    [Header("UI 元件")]
    [Tooltip("確定按鈕 (登出並回大廳)")]
    public Button confirmButton;

    [Tooltip("確認對話框 (需綁定)")]
    public ConfirmDialog confirmDialog;

    void Start()
    {
        // 確保載入最新分數
        GlobalVariables.LoadState();
        
        UpdateChartData();
        
        if (confirmButton != null) 
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    void UpdateChartData()
    {
        if (barChart == null) 
        {
            Debug.LogError("[ResultManager] BarChart 未綁定！請在 Inspector 拖入。");
            return;
        }

        // --- 1. 清除舊資料 ---
        var yAxis = barChart.GetChartComponent<YAxis>();
        if (yAxis != null)
        {
            yAxis.ClearData();
        }
        
        // 清空第一組數據 (Serie 0)
        Serie serie = null;
        if (barChart.series.Count > 0)
        {
            serie = barChart.series[0];
            serie.ClearData();
        }
        else
        {
            // 若沒有 Series，可考慮 AddSerie (目前假設場景已設定好)
            Debug.LogWarning("[ResultManager] BarChart 沒有 Series，無法顯示數據");
            return; 
        }

        // --- 2. 填入真實玩家數據 ---
        // 確保陣列不為空
        if (GlobalVariables.studentNames == null || GlobalVariables.playerCorrectCounts == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            // 安全檢查
            if (i >= GlobalVariables.studentNames.Length) break;
            if (i >= GlobalVariables.playerCorrectCounts.Length) break;

            string pName = GlobalVariables.studentNames[i];
            int correctCount = GlobalVariables.playerCorrectCounts[i];

            // 名字為空代表沒這個玩家，跳過
            if (string.IsNullOrEmpty(pName)) continue;

            // (A) 加入 Y 軸標籤 (玩家名字) -> 橫向圖表的 Category 在 Y 軸
            if (yAxis != null) yAxis.AddData(pName);

            // (B) 加入數值 (答對題數)
            if (serie != null)
            {
                serie.AddData(correctCount);
            }
        }

        // --- 3. 刷新圖表 ---
        barChart.RefreshChart();
    }

    void OnConfirmClicked()
    {
        // 顯示確認對話框
        if (confirmDialog != null)
        {
            confirmDialog.Show(
                "確定要登出並回到大廳？\n（資料將清空）",
                () => 
                {
                    // 確認後執行：清除狀態 -> 回大廳
                    GlobalVariables.ClearState();
                    SceneManager.LoadScene("LobbyScene");
                }
            );
        }
        else
        {
            // Fallback: 直接清除
            GlobalVariables.ClearState();
            SceneManager.LoadScene("LobbyScene");
        }
    }
}
