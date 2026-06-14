using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SucculentCommunity.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SucculentCommunity.Controllers
{
    public class HomeController : Controller
    {
        private readonly SucculentContext _context;

        // 注入資料庫連線工具
        public HomeController(SucculentContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 檢查目前登入的人是不是管理員？
            bool isAdmin = false;
            if (User.Identity.IsAuthenticated)
            {
                var currentAccount = User.Identity.Name;
                var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);
                if (currentMember != null) isAdmin = currentMember.IsAdmin;
            }
            ViewBag.IsAdmin = isAdmin; // 把結果打包放進 ViewBag 帶給畫面！

            // 進階加碼：去 Announcements 表抓出「最新的一筆公告」
            var latestAnnouncement = await _context.Announcements
                .OrderByDescending(a => a.PostTime)
                .FirstOrDefaultAsync();
            ViewBag.LatestAnnouncement = latestAnnouncement; // 打包帶給畫面！

            // 超級查詢魔法：去 Posts 表抓資料
            var posts = await _context.Posts
                .Include(p => p.Member) // 順便把發文的「園丁(Member)」資料打包帶走
                .Include(p => p.Plant)  // 順便把綁定的「植物(Plant)」資料打包帶走
                .Include(p => p.PostLikes)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                .Include(p => p.Favorites).ThenInclude(f => f.Member) // 順便把收藏的「園丁(Member)」資料打包帶走
                .Where(p => p.IsDeleted == false && p.IsPublic == "1") // 條件：沒被刪除 且 設定為公開
                .OrderByDescending(p => p.IsPinned) // 1. 先照置頂狀態排 ("1" 會排在 "0" 前面)
                .ThenByDescending(p => p.CreatedTime) // 2. 同樣狀態下，再照時間排新的在上面
                .ToListAsync();

            return View(posts); // 把打包好的貼文清單送到首頁畫面上！
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}