# Data

| 資料 | 路徑 | 格式與狀態 |
| --- | --- | --- |
| 20 題範例 | [drone_quiz.json](question-banks/drone_quiz.json) | 題目位於 `questions` 包裝內，原內容保留。 |
| 297 題活動題庫 | [題目.json](question-banks/workshop/題目.json) | 題號直接位於根節點，各難度 99 題。 |
| 答題紀錄 | [Research data](research/README.md) | 原始 JSON 與文件紀錄；來源 JSON 有解析問題。 |

這些 JSON 是部署與研究資料，不是 Unity 自動載入的資源。遊戲目前從 Firebase 根節點讀題；匯入前請閱讀 [Data formats](../docs/data-formats.md)。題庫的 DOCX 文件版本位於 [Teaching](../docs/teaching/README.md)。

[返回專案總覽](../README.md)
