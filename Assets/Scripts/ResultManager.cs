using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using XCharts.Runtime; // 引用 XCharts 核心

/// <summary>
/// 結算畫面管理器 (防呆加強版)
/// 負責讀取暫存的答題資料，並使用 XCharts 繪製長條圖
/// </summary>
public class ResultManager : MonoBehaviour
{
    #region Fields (變數宣告)

    [Header("圖表元件")]
    public BarChart scoreChart; 

    [Header("UI")]
    public Button backToLobbyButton;

    #endregion

    #region Unity Methods (Unity 生命週期)

    void Start()
    {
        // 確保按鈕有綁定事件
        if(backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(OnBackToLobby);

        // 開始計算並顯示
        CalculateAndShowData();
    }

    #endregion

    #region Private Methods (自定義邏輯)

    void CalculateAndShowData()
    {
        // --- 0. 防呆檢查：如果沒拉圖表，直接跳出避免報錯 ---
        if (scoreChart == null)
        {
            Debug.LogError("❌ [ResultManager] 錯誤：Inspector 裡的 'Score Chart' 欄位是空的！請把場景上的 BarChart 拉進去。");
            return;
        }

        // --- 1. 統計分數 ---
        Dictionary<string, int> scores = new Dictionary<string, int>();

        // 初始化分數 (過濾掉空名字)
        foreach (string name in GlobalVariables.studentNames)
        {
            if (!string.IsNullOrEmpty(name) && !scores.ContainsKey(name))
            {
                scores[name] = 0;
            }
        }

        // 計算答對題數
        foreach (AnswerData data in GlobalVariables.localAnswerHistory)
        {
            if (data.is_correct && scores.ContainsKey(data.student))
            {
                scores[data.student]++;
            }
        }

        // --- 2. 設定 XCharts ---
        scoreChart.ClearData(); // 清除舊數據

        // [防呆] 確保有標題元件
        var title = scoreChart.EnsureChartComponent<Title>();
        title.text = "小組成績結算";
        title.show = true;

        // [防呆] 確保有 Legend (圖例) 並隱藏它 (比較美觀)
        var legend = scoreChart.EnsureChartComponent<Legend>();
        legend.show = false;

        // [關鍵防呆] 檢查是否有 Serie，如果沒有就自動新增一個
        if (scoreChart.series.Count == 0)
        {
            Debug.Log("⚠️ 圖表缺少 Serie，自動新增一個 Bar Serie...");
            scoreChart.AddSerie<Bar>("學生分數");
        }

        // 取得第 0 個 Serie
        var serie = scoreChart.GetSerie<Bar>(0);
        if (serie != null)
        {
            // 確保有 Label 元件並開啟顯示
            var label = serie.EnsureComponent<LabelStyle>();
            label.show = true;
            label.position = LabelStyle.Position.Top;
        }

        // --- 3. 填入數據 ---
        int index = 0;
        int totalStudents = GlobalVariables.studentNames.Length;

        foreach (string name in GlobalVariables.studentNames)
        {
            // 如果名字是空的 (例如沒滿4人)，就跳過不畫
            if (string.IsNullOrEmpty(name)) continue;

            // 取得分數
            int score = scores.ContainsKey(name) ? scores[name] : 0;

            // 1. 設定 X 軸名字
            scoreChart.AddXAxisData(name);

            // 2. 設定 Y 軸分數
            // 因為上面已經確保了 series 至少有一個，這裡 AddData(0, ...) 就不會報錯了
            var serieData = scoreChart.AddData(0, score);

            // 3. 設定顏色 (彩虹漸層)
            if (serieData != null)
            {
                var itemStyle = serieData.EnsureComponent<ItemStyle>();
                // 參數：色相(0~1), 飽和度(0.7), 亮度(1)
                itemStyle.color = Color.HSVToRGB((float)index / totalStudents, 0.7f, 1.0f);
            }

            index++;
        }
    }

    void OnBackToLobby()
    {
        GlobalVariables.ResetGameData();
        SceneManager.LoadScene("LobbyScene");
    }

    #endregion
}