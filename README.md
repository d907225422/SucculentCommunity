# 🌵 SucculentCommunity (多肉植物社群與管理系統)

這是一個基於 **ASP.NET Core MVC (.NET 8)** 開發的社群管理系統。
雖然這是一個網站應用程式，但其底層的**關聯式資料庫設計 (RDBMS)**、**資料流防呆 (Validation)** 與 **MVC 架構分層** 思維，與工業自動化設備中的「配方(Recipe)管理」、「測試數據(Log)紀錄」與「HMI 介面防呆」的底層邏輯高度一致。

## 🛠️ 技術堆疊 (Tech Stack)
* **後端架構：** C#, ASP.NET Core MVC (.NET 8.0)
* **資料庫與 ORM：** MS SQL Server, Entity Framework Core 9.0
* **安全性與權限：** ASP.NET Core Cookie Authentication
* **前端與防呆：** HTML/CSS, Razor Views, jQuery, jQuery Validation (Unobtrusive)
* **進階套件：** `SixLabors.ImageSharp` (圖片處理), `X.PagedList` (資料分頁處理)

## 💡 核心亮點與工程思維 (Core Features)

### 1. 嚴謹且具彈性的資料庫關聯設計
* 透過 Entity Framework Core 實作多表關聯 (會員、植物圖鑑、貼文、留言)。
* 考量實際操作情境，精準設計 Foreign Key 的「強制約束 (NOT NULL)」(如貼文必屬某會員) 與「彈性空值 (Nullable)」(如建立檔案時植物種類可暫時留白)。
* **📌 設備開發對應：** 此概念等同於設備異常時，先即時寫入異常紀錄 (Nullable Error Code) 防止系統崩潰，後續再由工程師補齊錯誤代碼，確保資料庫的高可用性與完整性。

### 2. 雙重資料防呆機制 (Data Validation)
* **前端防呆：** 整合 `jQuery Validation` 進行客戶端即時驗證，減少不必要的伺服器請求。
* **後端防呆：** 透過 MVC Model Binding (Data Annotations) 進行二次過濾，阻擋惡意或錯誤格式寫入資料庫。
* **📌 設備開發對應：** 嚴格確保操作員在 HMI 介面輸入的生產參數 (Recipe) 絕對合法，避免錯誤參數導致機台發生撞機或當機。

### 3. 會員權限與登入狀態管理
* 實作基於 `CookieAuthenticationDefaults` 的身分驗證機制，未登入者會被系統強制導向或隱藏特定操作按鈕，確保系統安全性。

### 4. 檔案處理與大量資料優化
* 整合 `ImageSharp` 處理使用者上傳的圖片檔案。
* 導入 `X.PagedList` 實作資料列表分頁，避免資料庫一次撈取過多圖鑑或貼文導致記憶體與網路傳輸卡頓。
* **📌 設備開發對應：** 具備優化「巨量機台測試 Log 歷史紀錄」查詢介面的基礎能力，確保系統查詢效能。
