# PunishmentGame (Quiz Game)

這是一個基於 Unity 開發的多人問答遊戲專案，結合了 Firebase Realtime Database 進行題目管理與數據記錄，並使用 XCharts 進行結果視覺化。

## 專案簡介

此專案主要設計用於課堂或團體活動中的問答環節。玩家（學生）在遊戲開始前輸入名字，系統會將遊戲場次與玩家資訊上傳至 Firebase。遊戲過程中，題目會從雲端即時載入，玩家輪流或共同答題，系統會記錄答題正確率與作答時間。

## 主要功能

*   **大廳系統 (Lobby System)**:
    *   支援多位玩家輸入名字。
    *   自動建立遊戲場次 (Session) 並上傳至 Firebase。
*   **問答系統 (Quiz System)**:
    *   **雲端題庫**: 從 Firebase Realtime Database (`questions` 節點) 動態載入題目。
    *   **即時回饋**: 答題後立即顯示正確或錯誤，並播放對應音效。
    *   **輪替機制**: 支援輪流答題模式 (依據 `GlobalVariables.studentNames` 長度取餘數決定當前答題者)。
*   **數據記錄 (Data Logging)**:
    *   記錄每題的作答者、是否正確、作答耗時。
    *   數據即時同步回 Firebase (`game_sessions` 與 `answers` 節點)。
*   **結果展示**:
    *   遊戲結束後進入結算畫面 (ResultScene)。
    *   使用 XCharts 顯示統計圖表 (需確認 ResultScene 實作細節)。

## 專案結構

```
Assets/
├── Scenes/
│   ├── LobbyScene.unity    # 遊戲入口，輸入玩家名稱
│   ├── GameScene.unity     # 主要問答遊戲畫面
│   └── ResultScene.unity   # 遊戲結算與圖表展示
├── Scripts/
│   ├── FirebaseManager.cs  # 處理 Firebase 連線與數據上傳
│   ├── LobbyManager.cs     # 大廳邏輯，處理玩家名稱與 Session 建立
│   ├── QuizManager.cs      # 遊戲核心邏輯，載入題目與處理答題
│   ├── ResultManager.cs    # 結算畫面邏輯
│   ├── DataModels.cs       # 資料結構定義 (SessionData, AnswerData 等)
│   ├── GlobalVariables.cs  # 全域變數 (儲存玩家名單、資料庫 URL 等)
│   ├── Question.cs         # 題目資料結構
│   └── QuestionData.cs     # 題目相關資料結構
├── XCharts-master/         # XCharts 圖表套件
└── ...
```

## 安裝與設定 Requirements

1.  **Unity Version**: 建議使用 Unity 2021.3 或更高版本 (專案設定顯示為 Unity 2022/2023 相容)。
2.  **Firebase SDK**: 專案已包含 Firebase Database 與 Analytics 套件。
    *   需確保 `Assets/google-services.json` 設定檔正確對應您的 Firebase 專案。
3.  **XCharts**: 用於圖表繪製 (已包含在專案中)。
4.  **TextMesh Pro**: 用於 UI 文字顯示。

## 快速開始

1.  開啟 Unity Hub 並加入此專案。
2.  確認 `Assets/google-services.json` 是否存在且配置正確。
3.  開啟 `Assets/Scenes/LobbyScene.unity`。
4.  按下 Play 按鈕開始遊戲。
5.  輸入玩家名稱後點擊「開始遊戲」。

## Firebase 資料結構範例

**Questions (題目)**:
```json
{
  "questions": {
    "q1": {
      "questionText": "Unity 是什麼?",
      "options": ["遊戲引擎", "繪圖軟體", "文書工具", "作業系統"],
      "correctOptionIndex": 0,
      "difficultyLevel": 1
    }
  }
}
```

**Game Sessions (遊戲場次)**:
```json
{
  "game_sessions": {
    "session_id_123": {
      "timestamp": "2023-10-27 10:00:00",
      "students": ["PlayerA", "PlayerB"]
    }
  }
}
```

## 注意事項

*   請確保執行環境有網路連線，否則無法載入題目與上傳數據。
*   若遇到 Firebase 連線問題，請檢查 Bundle ID (`com.teacher.quizgame`) 是否與 Firebase Console 設定一致。

## 授權

此專案為教學或內部使用用途。
