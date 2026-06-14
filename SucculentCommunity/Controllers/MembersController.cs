using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SucculentCommunity.Data;
using SucculentCommunity.Models;
using SucculentCommunity.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using X.PagedList; // 🌟 引入分頁魔法套件
using X.PagedList.Extensions; // 🌟 引入擴充方法

namespace SucculentCommunity.Controllers
{
    public class MembersController : Controller
    {
        private readonly SucculentContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public MembersController(SucculentContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // ====================================================
        // 👑 後台管理區：會員列表 (僅限管理員)
        // ====================================================
        [Authorize]
        // 🌟 新增 int? page 參數
        public async Task<IActionResult> Index(string searchString, int? page)
        {
            // 🚨 後台門禁檢查
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin)
            {
                TempData["ErrorMessage"] = "⛔ 權限不足！只有系統管理員可以進入後台喔！";
                return RedirectToAction("Index", "Home");
            }

            ViewData["CurrentFilter"] = searchString;

            var members = from m in _context.Members select m;

            if (!String.IsNullOrEmpty(searchString))
            {
                members = members.Where(s => s.Account.Contains(searchString) || s.Nickname.Contains(searchString));
            }

            // 🌟 設定分頁規則
            int pageNumber = page ?? 1;
            int pageSize = 10; // 🌟 後台會員列表一頁顯示 10 筆剛剛好

            // 🌟 使用 ToPagedList 輸出分頁資料
            return View(members.OrderBy(m => m.MemberID).ToPagedList(pageNumber, pageSize));
        }

        // 👑 後台管理區：會員明細
        [Authorize]
        public async Task<IActionResult> Details(string id)
        {
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction("Index", "Home");

            if (id == null) return NotFound();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.MemberID == id);
            if (member == null) return NotFound();

            return View(member);
        }

        // 👑 後台管理區：編輯會員狀態 (GET)
        [Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction("Index", "Home");

            if (id == null) return NotFound();

            var member = await _context.Members.FindAsync(id);
            if (member == null) return NotFound();

            return View(member);
        }

        // 👑 後台管理區：編輯會員狀態 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        // 🌟 資安防護：這裡只綁定 MemberID 和 Status，防止管理員意外(或惡意)修改別人的密碼！
        public async Task<IActionResult> Edit(string id, [Bind("MemberID,Status")] Member member)
        {
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin) return RedirectToAction("Index", "Home");

            if (id != member.MemberID) return NotFound();

            // 去資料庫把原本的會員資料抓出來
            var existingMember = await _context.Members.FindAsync(id);
            if (existingMember == null) return NotFound();

            // 🌟 核心：管理員只能修改「帳號狀態」
            existingMember.Status = member.Status;

            _context.Update(existingMember);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✨ 會員 {existingMember.Account} 的狀態已更新！";
            return RedirectToAction(nameof(Index));
        }

        // ====================================================
        // 🚪 前台區：註冊、登入、登出
        // ====================================================

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Account,Password,Nickname")] Member member, string? avatarBase64)
        {
            ModelState.Remove("MemberID");
            ModelState.Remove("Avatar");
            ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                bool accountExists = _context.Members.Any(m => m.Account == member.Account);
                if (accountExists)
                {
                    ModelState.AddModelError("Account", "哎呀！這個帳號已經被註冊過囉，請換一個吧！");
                    return View(member);
                }

                var lastMember = _context.Members.OrderByDescending(m => m.MemberID).FirstOrDefault();
                if (lastMember == null)
                {
                    member.MemberID = "M0001";
                }
                else
                {
                    int lastIdNumber = int.Parse(lastMember.MemberID.Substring(1));
                    member.MemberID = "M" + (lastIdNumber + 1).ToString("D4");
                }

                if (!string.IsNullOrEmpty(avatarBase64))
                {
                    var base64Data = avatarBase64.Substring(avatarBase64.IndexOf(",") + 1);
                    byte[] imageBytes = Convert.FromBase64String(base64Data);

                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "avatars");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + ".jpg";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var image = Image.Load(imageBytes))
                    {
                        await image.SaveAsJpegAsync(filePath);
                    }
                    member.Avatar = uniqueFileName;
                }
                else
                {
                    member.Avatar = "default.jpg";
                }

                member.Status = "1";
                member.RegisterDate = DateTime.Now;

                _context.Add(member);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "🎉 註冊成功！快登入建立你的溫室吧！";
                return RedirectToAction("Login");
            }
            return View(member);
        }

        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string account, string password, string returnUrl = null)
        {
            var member = _context.Members.FirstOrDefault(m => m.Account == account && m.Password == password);

            if (member != null)
            {
                if (member.Status != "1")
                {
                    ViewData["ErrorMessage"] = "哎呀！您的帳號目前被停權囉！請聯繫管理員。";
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, member.Account),
                    new Claim("Nickname", member.Nickname),
                    new Claim(ClaimTypes.Role, member.IsAdmin ? "Admin" : "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                TempData["SuccessMessage"] = $"歡迎回來，{member.Nickname} 🪴";
                return RedirectToAction("Index", "Home");
            }

            ViewData["ErrorMessage"] = "帳號或密碼錯誤，請再試一次喔！";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ====================================================
        // ⚙️ 前台區：一般園丁編輯個人資料
        // ====================================================
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var member = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (member == null) return RedirectToAction("Login");

            var vm = new EditProfileVM
            {
                Account = member.Account,
                Nickname = member.Nickname,
                ExistingAvatar = member.Avatar
            };
            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileVM vm)
        {
            var member = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (member == null) return RedirectToAction("Login");

            vm.Account = member.Account;
            vm.ExistingAvatar = member.Avatar;

            if (ModelState.IsValid)
            {
                bool isPasswordChanged = false;

                if (!string.IsNullOrWhiteSpace(vm.NewPassword))
                {
                    if (string.IsNullOrWhiteSpace(vm.OldPassword) || member.Password != vm.OldPassword)
                    {
                        ViewData["ErrorMsg"] = "❌ 舊密碼輸入錯誤，無法修改密碼喔！";
                        return View(vm);
                    }
                    member.Password = vm.NewPassword;
                    isPasswordChanged = true;
                }

                member.Nickname = vm.Nickname;

                if (!string.IsNullOrEmpty(vm.CroppedAvatar))
                {
                    try
                    {
                        var base64Data = vm.CroppedAvatar.Split(',')[1];
                        byte[] imageBytes = Convert.FromBase64String(base64Data);

                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string fileName = Guid.NewGuid().ToString() + ".jpg";
                        string path = Path.Combine(uploadsFolder, fileName);

                        using (var image = Image.Load(imageBytes))
                        {
                            await image.SaveAsJpegAsync(path);
                        }
                        member.Avatar = fileName;
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("", "大頭貼處理失敗，請重新嘗試！");
                        return View(vm);
                    }
                }

                _context.Update(member);
                await _context.SaveChangesAsync();

                if (isPasswordChanged)
                {
                    TempData["SuccessMsg"] = "🎉 個人資料與密碼更新成功！下次請用新密碼登入喔 🔒";
                }
                else
                {
                    TempData["SuccessMsg"] = "🎉 個人資料更新成功囉！✨";
                }

                return RedirectToAction(nameof(EditProfile));
            }

            return View(vm);
        }
    }
}