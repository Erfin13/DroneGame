# 題庫與答題紀錄格式

## 題庫來源

| 資料 | 題數 | 最外層 | 用途 |
| --- | ---: | --- | --- |
| [冬令營題目 JSON](../%E5%9C%8B%E7%A7%91%E6%9C%83/02_%E6%B4%BB%E5%8B%95%E8%88%87%E6%95%99%E6%9D%90/02_%E9%A1%8C%E5%BA%AB/%E9%A1%8C%E7%9B%AE.json) | 297 | `qL1_01` 等題號直接位於根節點 | 根層級題庫；各難度 99 題。 |
| [20 題範例](../data/question-banks/drone_quiz.json) | 20 | `questions` 內放置題目 | 原有題庫範例；保留原始包裝。 |

目前 `QuizManager.LoadQuestionsFromFirebase()` 讀取資料庫 Root 的每個 Child，將 Child 當成一題，不會自動向下讀取 `questions`。使用 20 題範例時，应將其 `questions` 內的題目對應到程式預期的根層級，或另外修改讀取邏輯；本次只整理檔案，沒有改寫資料庫或程式。

## 題目欄位

| 欄位 | 型別 | 說明 |
| --- | --- | --- |
| 題目 key | 字串 | 作答紀錄的 `question_id`；例如 `qL1_01`。 |
| `questionText` | 字串 | 題幹。 |
| `options` | 字串陣列 | 選項文字。 |
| `correctOptionIndex` | 整數 | 從 0 起算的正確選項位置。 |
| `difficultyLevel` | 整數 | 1、2、3；由掃描難度篩選。 |
| `type` | 字串，選填 | 執行期模型支援 `quiz`、`supply`；缺少時預設 `quiz`。 |
| `reward` | 整數，選填 | 能量獎勵；實際預設值以 `QuizManager` 為準。 |

`QuestionData` 是 Unity ScriptableObject 題目模型，與 Firebase 執行期 `Question` 分開保留。

## 掃描入口

QR Code 以 `qid=qL1`、`qid=qL2`、`qid=qL3` 指定難度；`ScanSceneController` 也接受帶有 `qid=` 參數的網址或以 `|` 分隔的內容。題庫的單一題號不能當成掃描入口。抽題依難度記錄已使用題號；該難度用完後重置集合。

## 場次與作答

場次位置為 `game_sessions/{sessionID}`，答題位置為其 `answers/{pushID}`。依 `DataModels.cs`：

| 資料模型 | 欄位 |
| --- | --- |
| `SessionData` | `timestamp`、`students` |
| `AnswerData` | `question_id`、`student`、`is_correct`、`time_taken`、`timestamp` |

`ResultManager` 從當前場次的 `answers` 讀取作答，統計玩家答對數並繪圖。

## 原始紀錄狀態

[成果檔案 JSON](../%E5%9C%8B%E7%A7%91%E6%9C%83/03_%E8%B3%87%E6%96%99%E8%88%87%E5%88%86%E6%9E%90/02_%E7%AD%94%E9%A1%8C%E7%B4%80%E9%8C%84/%E6%88%90%E6%9E%9C%E6%AA%94%E6%A1%88.json) 原始內容在第 **978 行、第 5 欄**無法通過標準 JSON 解析，錯誤為預期帶雙引號的屬性名稱。此檔保留原始內容與 SHA-256，沒有自動補齊括號、推測遺漏紀錄或將其轉成已驗證的資料集。

`學生桌遊答題狀況.docx` 是另外保存的答題紀錄文件。前測、後測 DOCX/PDF 為統計檢測報告，與原始答題 JSON 分開編排。

[返回專案總覽](../README.md)
