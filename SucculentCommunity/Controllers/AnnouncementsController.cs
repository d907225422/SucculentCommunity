using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SucculentCommunity.Data;
using SucculentCommunity.Models;
using X.PagedList; // 🌟 引入分頁魔法套件
using X.PagedList.Extensions; // 🌟 引入擴充方法 (解決報錯的關鍵)

namespace SucculentCommunity.Controllers
{
    public class AnnouncementsController : Controller
    {
        private readonly SucculentContext _context;

        public AnnouncementsController(SucculentContext context)
        {
            _context = context;
        }

        // GET: Announcements (公告大廳)
        [AllowAnonymous] // 允許所有人查看公告
        public async Task<IActionResult> Index(int? page) // 🌟 新增 int? page 參數
        {
            // 🌟 檢查目前登入者是否為管理員，決定是否顯示「新增公告」按鈕
            bool isAdmin = false;
            if (User.Identity.IsAuthenticated)
            {
                var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
                if (currentMember != null) isAdmin = currentMember.IsAdmin;
            }
            ViewBag.IsAdmin = isAdmin;

            // 🌟 設定分頁規則：如果有傳 page 就用那一頁，沒有就預設第 1 頁
            int pageNumber = page ?? 1;
            int pageSize = 7; // 🌟 公告我們設定一頁顯示 7 筆

            // 🌟 將公告依時間「由新到舊」排序，並直接呼叫 ToPagedList() 進行分頁
            var announcements = _context.Announcements
                .OrderByDescending(a => a.PostTime)
                .ToPagedList(pageNumber, pageSize);

            return View(announcements);
        }

        // GET: Announcements/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            bool isAdmin = false;
            if (User.Identity.IsAuthenticated)
            {
                var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
                if (currentMember != null) isAdmin = currentMember.IsAdmin;
            }
            ViewBag.IsAdmin = isAdmin;

            var announcement = await _context.Announcements.FirstOrDefaultAsync(m => m.AnnounceID == id);
            if (announcement == null) return NotFound();

            return View(announcement);
        }

        // ========================================================
        // 👑 以下為管理員 (Admin) 專屬領域
        // ========================================================

        // GET: Announcements/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction(nameof(Index));

            return View();
        }

        // POST: Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("AnnounceID,Title,Content")] Announcement announcement)
        {
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction(nameof(Index));

            if (ModelState.IsValid)
            {
                announcement.PostTime = DateTime.Now; // 🌟 系統自動押上發佈時間
                _context.Add(announcement);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "📢 系統公告發佈成功！";
                return RedirectToAction(nameof(Index));
            }
            return View(announcement);
        }

        // GET: Announcements/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(long? id)
        {
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction(nameof(Index));

            if (id == null) return NotFound();

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            return View(announcement);
        }

        // POST: Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(long id, [Bind("AnnounceID,Title,Content,PostTime")] Announcement announcement)
        {
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction(nameof(Index));

            if (id != announcement.AnnounceID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(announcement);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "✨ 公告修改成功！";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.AnnounceID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Details), new { id = announcement.AnnounceID });
            }
            return View(announcement);
        }

        // POST: Announcements/Delete/5 (改為直接用 POST 刪除，省去確認畫面)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction(nameof(Index));

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "🗑️ 系統公告已刪除！";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AnnouncementExists(long id)
        {
            return _context.Announcements.Any(e => e.AnnounceID == id);
        }
    }
}