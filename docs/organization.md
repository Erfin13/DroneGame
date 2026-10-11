# Repository organization

## 目錄規則

頂層目錄按專案用途編排：`Assets/`、`Packages/`、`ProjectSettings/` 為 Unity 專案；`docs/` 存放設計、操作、計畫與範本；`data/` 保存題庫及原始紀錄；`reports/` 保存分析與成果報告；`papers/` 保存論文與參考文獻；`artifacts/` 保存原有除錯產物。

資料夾使用用途明確的英文名稱，不沿用個人本機資料夾名或行政單位名稱。原始中文檔名保留，避免改變文件辨識與來源追溯。研究資料的入口在 [Research guide](research/README.md)，並整合到各功能目錄。

## 版本與保留

原稿、修訂稿、不同格式與同內容來源副本均各自保留。計畫來源副本位於 `source-copies/` 或 `source-reference/`，參考專題的不同來源以子目錄區分；不以修改時間判定定稿版本。

程式按 Core、Models、Infrastructure、Features、UI 分層，13 個腳本的 `.meta` 與 GUID 保留。Unity 根目錄、場景、第三方資源及專案設定保持原位置。PDF 沿用 Git LFS，活動題庫 JSON 與原始研究紀錄禁止換行正規化，保留原始位元組。

## 來源與搬移清單

| 清單 | 用途 |
| --- | --- |
| [Research file manifest](archive/research-files.json) | 36 個研究原檔的來源、現行位置、大小與 SHA-256。 |
| [File index](archive/file-index.md) | 可點選的完整原始檔案索引。 |
| [Versions](archive/versions.md) | 文件版本、格式及同內容副本。 |
| [Cumulative file moves](file-moves.json) | 從最初整理前提交起的 64 個原檔搬移對照。 |
| [Repository layout moves](repository-layout-moves.json) | 本次移出本機命名目錄的 46 個檔案對照。 |
| [Verification results](verification-results.json) | 原始檔保留、連結與 Unity 引用檢查結果。 |

最初 1,615 個受追蹤檔案仍全部保留。重新編排的 README 之整理前內容保存於 [Legacy README](legacy/README-before-organization.md)。歷史清單中的舊路徑用於追溯，現行路徑以各清單的 `path` 欄位為準。

本次檢查檔案雜湊、Git 暫存內容、來源副本、Markdown 連結與 Unity GUID；沒有啟動 Unity、相機或 Firebase 執行測試，也沒有重新計算研究統計。

[返回專案總覽](../README.md)
