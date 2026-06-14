using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SucculentCommunity.Data;
using SucculentCommunity.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList; // 🌟 引入分頁魔法套件
using X.PagedList.Extensions; // 🌟 新增這行！解鎖最新版的分頁擴充方法

namespace SucculentCommunity.Controllers
{
    // 🌟 全站預設：要登入才能看圖鑑，但只有 Admin 可以新增和修改！
    [Authorize]
    public class SpeciesController : Controller
    {
        private readonly SucculentContext _context;
        private readonly IWebHostEnvironment _hostEnvironment; // 🌟 用來取得 wwwroot 實體路徑的超級工具

        // 建構子：把資料庫跟環境工具注入進來
        public SpeciesController(SucculentContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Species (圖鑑列表)
        [AllowAnonymous] // 🌟 允許沒登入的路人也可以看圖鑑
        // 🌟 1. 新增 int? page 參數
        public async Task<IActionResult> Index(string searchString, string searchFamily, int? page)
        {
            // 🌟 核心防護與畫面分流：檢查目前登入者是否為管理員
            bool isAdmin = false;
            if (User.Identity.IsAuthenticated)
            {
                var currentAccount = User.Identity.Name;
                var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);
                if (currentMember != null) isAdmin = currentMember.IsAdmin;
            }
            ViewBag.IsAdmin = isAdmin; // 把權限打包帶給前端！

            // 1. 去資料庫把所有出現過的「科別」抓出來，去掉重複的，準備給前端當下拉選單！
            var familyQuery = from m in _context.Species
                              orderby m.Family
                              select m.Family;
            // 🌟 順便把 searchFamily 傳進去，讓下拉選單記住目前選了什麼
            ViewBag.SearchFamily = new SelectList(await familyQuery.Distinct().ToListAsync(), searchFamily);

            // 2. 把使用者輸入的條件記下來，讓畫面的輸入框不會被清空
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentFamily"] = searchFamily; // 🌟 記住下拉選單選了什麼科

            // 3. 準備基礎查詢：去資料庫準備撈圖鑑
            var species = from s in _context.Species
                          select s;

            // 4. 魔法過濾 1：如果有從下拉選單選「科別」，就只留那個科別的
            if (!string.IsNullOrEmpty(searchFamily))
            {
                species = species.Where(x => x.Family == searchFamily);
            }

            // 5. 魔法過濾 2：如果有打字搜尋，就去找「中文名稱」或「學名」有沒有包含這個字
            if (!string.IsNullOrEmpty(searchString))
            {
                species = species.Where(s => s.CommonName.Contains(searchString) || s.ScientificName.Contains(searchString));
            }

            // 🌟 6. 設定分頁規則：如果有傳 page 就用那一頁，沒有就預設第 1 頁。每頁顯示 12 筆！
            int pageNumber = page ?? 1;
            int pageSize = 8;

            // 最後把過濾完的資料排序好，送給前端畫面！(🌟 改用 ToPagedListAsync)
            return View(species.OrderBy(s => s.SpeciesID).ToPagedList(pageNumber, pageSize));
        }

        // GET: Species/Details/5 (圖鑑明細)
        [AllowAnonymous]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            // 🌟 核心功能：檢查目前登入者是否為管理員 ＆ 是否已收藏此品種
            bool isAdmin = false;
            bool isFavorited = false;

            if (User.Identity.IsAuthenticated)
            {
                var currentAccount = User.Identity.Name;
                var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);

                if (currentMember != null)
                {
                    isAdmin = currentMember.IsAdmin; // 🌟 取得管理員狀態

                    // 取得收藏狀態
                    isFavorited = await _context.Favorites.AnyAsync(f =>
                        f.SpeciesID == id &&
                        f.MemberID == currentMember.MemberID &&
                        f.FavType == "Species");
                }
            }

            ViewBag.IsAdmin = isAdmin;         // 🌟 傳給前端判斷是否顯示編輯/刪除按鈕
            ViewBag.IsFavorited = isFavorited; // 傳給前端決定收藏按鈕顏色

            var species = await _context.Species.FirstOrDefaultAsync(m => m.SpeciesID == id);
            if (species == null) return NotFound();

            // ==========================================
            // 🌟 新增魔法：上一筆 / 下一筆 導航邏輯
            // (Entity Framework 會自動把 string.Compare 轉換成 SQL 的大於小於比較)
            // ==========================================

            // 1. 取得「上一筆」(圖鑑編號較小的那一筆，例如 S0002 的上一筆是 S0001)
            var prevSpecies = await _context.Species
                .Where(s => string.Compare(s.SpeciesID, id) < 0)
                .OrderByDescending(s => s.SpeciesID)
                .FirstOrDefaultAsync();
            ViewBag.PrevId = prevSpecies?.SpeciesID; // 打包帶給畫面

            // 2. 取得「下一筆」(圖鑑編號較大的那一筆，例如 S0002 的下一筆是 S0003)
            var nextSpecies = await _context.Species
                .Where(s => string.Compare(s.SpeciesID, id) > 0)
                .OrderBy(s => s.SpeciesID)
                .FirstOrDefaultAsync();
            ViewBag.NextId = nextSpecies?.SpeciesID; // 打包帶給畫面

            return View(species);
        }

        // ========================================================
        // 🌟 AJAX 無痕收藏品種功能 (對應你的 Favorite 資料表結構)
        // ========================================================
        [HttpPost]
        [AllowAnonymous] // 🌟 讓訪客能順利收到 JSON 訊息，觸發轉跳登入頁
        public async Task<IActionResult> ToggleFavorite(string speciesId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "請先登入後再收藏品種喔！" });
            }

            var currentAccount = User.Identity.Name;
            var member = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);
            if (member == null) return Json(new { success = false, message = "找不到會員資料。" });

            // 尋找是否已存在這筆收藏紀錄
            var favorite = await _context.Favorites.FirstOrDefaultAsync(f =>
                f.SpeciesID == speciesId &&
                f.MemberID == member.MemberID &&
                f.FavType == "Species");

            bool isFavorited;

            if (favorite == null)
            {
                // 若未收藏，則新增一筆紀錄
                var newFavorite = new Favorite
                {
                    MemberID = member.MemberID,
                    SpeciesID = speciesId,
                    FavType = "Species", // 🌟 標記類型為圖鑑
                    PostID = null        // 貼文 ID 為空
                };
                _context.Favorites.Add(newFavorite);
                isFavorited = true;
            }
            else
            {
                // 若已收藏，則移除紀錄 (取消收藏)
                _context.Favorites.Remove(favorite);
                isFavorited = false;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, isFavorited = isFavorited });
        }

        // ========================================================
        // 👑 以下為管理員 (Admin) 專屬領域
        // ========================================================

        // GET: Species/Create (顯示新增畫面)
        [Authorize]
        public async Task<IActionResult> Create()
        {
            // 🌟 手動檢查權限：去資料庫查這個人是不是管理員？
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin)
            {
                TempData["ErrorMessage"] = "權限不足！只有管理員可以新增圖鑑喔！";
                return RedirectToAction(nameof(Index));
            }

            // 🌟 自動產生品種代碼 (例如：S0001)
            string newId = "S0001";
            var lastSpecies = _context.Species.OrderByDescending(s => s.SpeciesID).FirstOrDefault();

            if (lastSpecies != null)
            {
                string lastIdNumStr = lastSpecies.SpeciesID.Substring(1);
                if (int.TryParse(lastIdNumStr, out int lastIdNum))
                {
                    newId = "S" + (lastIdNum + 1).ToString("D4");
                }
            }

            var newSpecies = new Species { SpeciesID = newId };
            return View(newSpecies);
        }

        // POST: Species/Create (接收新增資料與圖片)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("SpeciesID,CommonName,ScientificName,Family,Genus,GrowthSeason,LightGuide,WaterGuide,SoilMix,Description")] Species species, IFormFile? uploadImage)
        {
            // 🌟 手動檢查權限
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction(nameof(Index));

            if (ModelState.IsValid)
            {
                // 🚨 防護 1：檢查「中文名稱」是否重複
                if (_context.Species.Any(s => s.CommonName == species.CommonName))
                {
                    ModelState.AddModelError("CommonName", "這盆多肉已經存在於圖鑑裡囉！請確認是否重複建檔。");
                    return View(species);
                }

                // 📸 防護 2：處理圖片上傳 (優先處理 Cropper.js 傳來的 Base64)
                string croppedImage = Request.Form["CroppedImage"]; // 🌟 接收前端裁切字串

                if (!string.IsNullOrEmpty(croppedImage))
                {
                    var base64Data = croppedImage.Contains(",") ? croppedImage.Split(',')[1] : croppedImage;
                    byte[] imageBytes = Convert.FromBase64String(base64Data);
                    string fileName = Guid.NewGuid().ToString() + ".jpg";
                    string path = Path.Combine(_hostEnvironment.WebRootPath, "images", "species", fileName);
                    await System.IO.File.WriteAllBytesAsync(path, imageBytes);
                    species.SpeciesImg = fileName;
                }
                // 🌟 備用方案：如果園丁只選了檔案卻取消裁切，還是能成功存檔！
                else if (uploadImage != null && uploadImage.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + ".jpg";
                    string path = Path.Combine(_hostEnvironment.WebRootPath, "images", "species", fileName);
                    using (var image = await Image.LoadAsync(uploadImage.OpenReadStream()))
                    {
                        await image.SaveAsJpegAsync(path);
                    }
                    species.SpeciesImg = fileName;
                }
                else
                {
                    species.SpeciesImg = "default.jpg";
                }

                _context.Add(species);
                await _context.SaveChangesAsync();

                // 🌟 新增成功提示
                TempData["SuccessMessage"] = "🎉 新圖鑑建立成功！";
                return RedirectToAction(nameof(Index));
            }
            return View(species);
        }

        // GET: Species/Edit/5 (顯示編輯畫面)
        [Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            // 🌟 手動檢查權限
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin)
            {
                TempData["ErrorMessage"] = "權限不足！只有管理員可以編輯圖鑑喔！";
                return RedirectToAction(nameof(Index));
            }

            if (id == null) return NotFound();
            var species = await _context.Species.FindAsync(id);
            if (species == null) return NotFound();
            return View(species);
        }

        // POST: Species/Edit/5 (接收編輯資料與新圖片)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(string id, [Bind("SpeciesID,CommonName,ScientificName,Family,Genus,GrowthSeason,LightGuide,WaterGuide,SoilMix,Description,SpeciesImg")] Species species, IFormFile? uploadImage)
        {
            // 🌟 手動檢查權限
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction(nameof(Index));

            if (id != species.SpeciesID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 🚨 檢查名稱是否重複 (但排除自己，否則改個錯字就存不了)
                    if (_context.Species.Any(s => s.CommonName == species.CommonName && s.SpeciesID != species.SpeciesID))
                    {
                        ModelState.AddModelError("CommonName", "這個名稱已經被其他圖鑑使用了！");
                        return View(species);
                    }

                    // 📸 處理換新圖片的邏輯 (優先處理 Cropper.js 傳來的 Base64)
                    string croppedImage = Request.Form["CroppedImage"]; // 🌟 接收前端裁切字串

                    if (!string.IsNullOrEmpty(croppedImage))
                    {
                        var base64Data = croppedImage.Contains(",") ? croppedImage.Split(',')[1] : croppedImage;
                        byte[] imageBytes = Convert.FromBase64String(base64Data);
                        string fileName = Guid.NewGuid().ToString() + ".jpg";
                        string path = Path.Combine(_hostEnvironment.WebRootPath, "images", "species", fileName);
                        await System.IO.File.WriteAllBytesAsync(path, imageBytes);
                        species.SpeciesImg = fileName;
                    }
                    // 🌟 備用方案：如果園丁只選了檔案卻取消裁切
                    else if (uploadImage != null && uploadImage.Length > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + ".jpg";
                        string path = Path.Combine(_hostEnvironment.WebRootPath, "images", "species", fileName);
                        using (var image = await Image.LoadAsync(uploadImage.OpenReadStream()))
                        {
                            await image.SaveAsJpegAsync(path);
                        }
                        species.SpeciesImg = fileName;
                    }

                    _context.Update(species);
                    await _context.SaveChangesAsync();

                    // 🌟 編輯成功提示，並導回該植物的明細頁
                    TempData["SuccessMessage"] = "✨ 圖鑑資料更新成功！";
                    return RedirectToAction(nameof(Details), new { id = species.SpeciesID });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpeciesExists(species.SpeciesID)) return NotFound();
                    else throw;
                }
            }
            return View(species);
        }

        // ========================================================
        // 🗑️ 安全刪除功能
        // ========================================================

        // GET: Species/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            // 🌟 手動檢查權限
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin)
            {
                TempData["ErrorMessage"] = "權限不足！只有管理員可以刪除圖鑑喔！";
                return RedirectToAction(nameof(Index));
            }

            if (id == null) return NotFound();
            var species = await _context.Species.FirstOrDefaultAsync(m => m.SpeciesID == id);
            if (species == null) return NotFound();
            return View(species);
        }

        // POST: Species/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            // 🌟 手動檢查權限
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction(nameof(Index));

            var species = await _context.Species.FindAsync(id);
            if (species != null)
            {
                try
                {
                    _context.Species.Remove(species);
                    await _context.SaveChangesAsync();

                    // 🌟 刪除成功提示
                    TempData["SuccessMessage"] = "🗑️ 圖鑑已成功刪除！";
                }
                catch (DbUpdateException)
                {
                    // 🚨 終極安全氣囊：如果已經有園丁在種這盆植物了 (觸發外鍵約束)
                    TempData["ErrorMessage"] = "已有園丁收藏或種植此品種，為了保護園丁的資料，無法刪除喔！建議改用「編輯」修改內容。";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SpeciesExists(string id)
        {
            return _context.Species.Any(e => e.SpeciesID == id);
        }
    }
}