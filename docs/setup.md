# 環境設定與操作流程

## 開啟專案

1. 安裝並使用專案指定的 Unity **2022.3.36f1**；版本來源為 `ProjectSettings/ProjectVersion.txt`。
2. 以 Git clone 此儲存庫，並使用 Git LFS 取得二進位資源：

   ```powershell
   git clone https://github.com/Erfin13/DroneGame.git
   cd DroneGame
   git lfs install
   git lfs pull
   ```

3. 在 Unity Hub 加入儲存庫根目錄。`Assets`、`Packages`、`ProjectSettings` 應位於此目錄。
4. 等待套件與資源匯入；套件清單以 `Packages/manifest.json` 為準。專案已有 Firebase SDK、XCharts、TextMesh Pro 與相機掃描相關 Plugins。

## Firebase 與題庫

`GlobalVariables.DATABASE_URL` 為目前程式使用的資料庫網址。確認 Firebase 專案、平台設定與存取規則對應實際執行環境。Android 的 `Assets/google-services.json` 已被既有 `.gitignore` 排除，需要由專案管理者配置；其他既有平台設定留在原資源位置。

目前 `QuizManager` 讀取資料庫根節點下的題目。冬令營 297 題 JSON 是根層級題庫；`data/question-banks/drone_quiz.json` 則以 `questions` 包裝 20 題。兩者不可直接混用，匯入前依[資料格式說明](data-formats.md)選擇對應位置。

## 操作流程

1. 開啟 `Assets/Scenes/LobbyScene.unity`，進入 Play 模式並輸入玩家。
2. 建立場次後選擇本次作答玩家。
3. 允許相機存取，掃描包含 `qid=qL1`、`qid=qL2` 或 `qid=qL3` 的 QR Code，例如 `QUIZ|qid=qL1`。
4. 遊戲依難度選取尚未使用的題目，顯示作答回饋並保存紀錄；完成後回到玩家選擇。
5. 結束場次進入結果畫面，查看作答統計，再返回大廳。

程式明確拒絕 `qL1_01` 這類固定題號作為掃描入口。若資料庫缺少該難度題目、沒有選擇難度或網路連線失敗，問答畫面會提示並返回玩家選擇。

## 整理後檢查範圍

本次完成檔案完整性與引用的靜態檢查，沒有啟動 Unity 執行遊戲或連線測試 Firebase。第一次開啟時可依序確認大廳、玩家選擇、相機掃描、答題上傳與結果圖表。

[返回專案總覽](../README.md)
