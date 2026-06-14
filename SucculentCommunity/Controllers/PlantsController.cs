using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SucculentCommunity.Data;
using SucculentCommunity.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SucculentCommunity.Controllers
{
    [Authorize] // 🌟 魔法防護罩：這座溫室是私人的，必須登入才能進來！
    public class PlantsController : Controller
    {
        private readonly SucculentContext _context;
        private readonly IWebHostEnvironment _hostEnvironment; // 🌟 取得 wwwroot 路徑的工具

        public PlantsController(SucculentContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Plants/Index (顯示我的專屬溫室)
        public async Task<IActionResult> Index()
        {
            // 1. 先確認現在是哪位園丁在門口
            var currentAccount = User.Identity.Name;
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);

            if (currentMember == null) return RedirectToAction("Login", "Members");

            // 2. 去資料庫把「屬於這位園丁」且「活著(沒被刪除)」的植物全部找出來！
            // 🌟 最重要的是：順便用 Include 把圖鑑(Species)的資料一起打包，這樣才知道它是什麼品種！
            var myPlants = await _context.Plants
                .Include(p => p.Species)
                .Where(p => p.MemberID == currentMember.MemberID && p.IsDeleted == false)
                .OrderByDescending(p => p.CreateDate) // 最晚帶回家的排在最前面
                .ToListAsync();

            return View(myPlants); // 把植物圖鑑打包送給畫面！
        }

        // ====================================================
        // ✏️ 編輯植物、修改日期與換大頭貼功能
        // ====================================================

        // GET: Plants/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var plant = await _context.Plants.Include(p => p.Member).FirstOrDefaultAsync(p => p.PlantID == id && !p.IsDeleted);
            if (plant == null) return NotFound();

            // 🚨 權限防護：如果不是這個園丁的植物，踢回溫室！
            if (plant.Member.Account != User.Identity.Name) return RedirectToAction(nameof(Index));

            // 準備圖鑑品種的下拉選單
            ViewBag.SpeciesID = new SelectList(_context.Species.OrderBy(s => s.SpeciesID), "SpeciesID", "CommonName", plant.SpeciesID);

            var vm = new EditPlantVM
            {
                PlantID = plant.PlantID,
                SpeciesID = plant.SpeciesID,
                PlantNickname = plant.PlantNickname,
                CreateDate = plant.CreateDate, // 🌟 GET: 把舊的日期裝進箱子送給畫面
                ExistingImage = plant.PlantImage
            };

            return View(vm);
        }

        // POST: Plants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EditPlantVM vm)
        {
            if (id != vm.PlantID) return NotFound();

            var plant = await _context.Plants.Include(p => p.Member).FirstOrDefaultAsync(p => p.PlantID == id && !p.IsDeleted);
            if (plant == null) return NotFound();

            if (plant.Member.Account != User.Identity.Name) return RedirectToAction(nameof(Index));

            if (ModelState.IsValid)
            {
                plant.PlantNickname = vm.PlantNickname;
                plant.SpeciesID = vm.SpeciesID;
                plant.CreateDate = vm.CreateDate; // 🌟 POST: 把畫面傳來的新日期存進資料庫！

                // ====================================================
                // 📸 處理前端傳來的裁切後 Base64 圖片字串 (Cropper.js)
                // ====================================================
                if (!string.IsNullOrEmpty(vm.CroppedImage))
                {
                    try
                    {
                        // 1. 移除 Base64 字串前方的格式標頭 (例如: "data:image/jpeg;base64,")
                        var base64Data = vm.CroppedImage;
                        if (base64Data.Contains(","))
                        {
                            base64Data = base64Data.Split(',')[1];
                        }

                        // 2. 將乾淨的 Base64 字串轉換回 Byte 陣列 (二進位圖片資料)
                        byte[] imageBytes = Convert.FromBase64String(base64Data);

                        // 3. 準備存檔路徑 (存到 avatars 資料夾)
                        var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "avatars");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        // 4. 產生全新不重複的檔名
                        string fileName = Guid.NewGuid().ToString() + ".jpg";
                        string path = Path.Combine(uploadsFolder, fileName);

                        // 5. 將圖片二進位資料直接寫入伺服器硬碟！
                        await System.IO.File.WriteAllBytesAsync(path, imageBytes);

                        // 6. 更新資料庫裡的圖片檔名
                        plant.PlantImage = fileName;
                    }
                    catch (Exception ex)
                    {
                        // 萬一解析失敗，加上一個錯誤提示給前端
                        ModelState.AddModelError("CroppedImage", "圖片處理發生錯誤，請重新嘗試！錯誤訊息：" + ex.Message);
                        ViewBag.SpeciesID = new SelectList(_context.Species.OrderBy(s => s.SpeciesID), "SpeciesID", "CommonName", vm.SpeciesID);
                        return View(vm);
                    }
                }
                // 🚨 雙重防呆：如果園丁的瀏覽器不支援 Cropper，我們還是保留原本上傳 IFormFile 的備用存檔方案
                else if (vm.UploadImage != null && vm.UploadImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "avatars");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid().ToString() + ".jpg";
                    string path = Path.Combine(uploadsFolder, fileName);

                    using (var image = await Image.LoadAsync(vm.UploadImage.OpenReadStream()))
                    {
                        await image.SaveAsJpegAsync(path);
                    }
                    plant.PlantImage = fileName;
                }

                _context.Update(plant);
                await _context.SaveChangesAsync();

                // 🌟 改完後帶他回溫室看成果，並且加上修改成功的 Toast 提示！
                TempData["SuccessMessage"] = "✨ 溫室寶貝資料更新成功！";
                return RedirectToAction(nameof(Index));
            }

            // 如果必填欄位沒填好，把下拉選單重新包裝還給畫面，避免網頁崩潰
            ViewBag.SpeciesID = new SelectList(_context.Species.OrderBy(s => s.SpeciesID), "SpeciesID", "CommonName", vm.SpeciesID);
            return View(vm);
        }

        // ====================================================
        // 🗑️ 刪除植物功能 (連動軟刪除相關貼文)
        // ====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var plant = await _context.Plants.Include(p => p.Member).FirstOrDefaultAsync(p => p.PlantID == id);

            // 防呆與防駭檢查
            if (plant == null || plant.Member.Account != User.Identity.Name) return RedirectToAction(nameof(Index));

            // 🌟 1. 把植物標記為「已刪除」
            plant.IsDeleted = true;
            _context.Update(plant);

            // 🌟 2. 連動刪除魔法：找出所有綁定這盆植物的貼文
            var relatedPosts = await _context.Posts.Where(p => p.PlantID == id && p.IsDeleted == false).ToListAsync();

            // 🌟 3. 把這些貼文也全部標記為「已刪除」
            foreach (var post in relatedPosts)
            {
                post.IsDeleted = true;
                _context.Update(post);
            }

            // 🌟 4. 一口氣儲存所有變更！
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "🗑️ 植物與相關日誌已順利移除！";
            return RedirectToAction(nameof(Index));
        }
    }
}