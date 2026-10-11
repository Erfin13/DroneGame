# Contributing

## 選擇存放位置

| 內容 | 位置 |
| --- | --- |
| Unity 程式與資源 | `Assets/`，腳本依責任放入 `Scripts/` 的既有分層。 |
| 設計、操作、研究計畫與教學 | `docs/` 的對應子目錄。 |
| 題庫與原始研究紀錄 | `data/`，保留原始資料版本。 |
| 分析與成果報告 | `reports/`，區分分析階段與文件版本。 |
| 投稿論文與參考文獻 | `papers/`，論文區分原稿及修訂稿，文獻依主題分類。 |

資料夾採用途明確的英文名稱。既有來源文件保留原始檔名；新版本以子目錄或明確版本標記區分，不覆寫原稿與來源副本。

## Unity 資源

使用 Unity 2022.3.36f1，透過 Unity Editor 或成對搬移資源及 `.meta`，保留既有 GUID。新增 Unity 資源與資料夾需包含 `.meta`。程式目錄分類不改變場景、Resources 或 StreamingAssets 的既有路徑規則。

PDF 與既有二進位格式使用 Git LFS；提交前確認 LFS 物件完整。遊戲資料庫格式與 QR 難度規則見 [Data formats](docs/data-formats.md)。

## 文件與資料

新增或搬移來源文件時，同步更新分類 README、[檔案索引](docs/archive/file-index.md)及[來源清單](docs/archive/research-files.json)，記錄版本、現行路徑與 SHA-256。研究原始紀錄保留原樣；若需清理或修復，新增衍生版本並記錄其來源。

提交前確認相對連結存在、搬移前後檔案內容一致、Unity GUID 與場景引用有效。檢查涉及執行行為的變更時，以實際 Unity 測試結果說明驗證範圍。
