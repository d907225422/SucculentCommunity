using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SucculentCommunity.Data;
using SucculentCommunity.Models;
using SucculentCommunity.ViewModels; // 🌟 記得引入我們剛剛建好的 ViewModel
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList; // 🌟 引入分頁魔法套件
using X.PagedList.Extensions; // 🌟 引入擴充方法

namespace SucculentCommunity.Controllers
{
    [Authorize] // 🌟 確保只有登入的園丁才能發文！
    public class PostsController : Controller
    {
        private readonly SucculentContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public PostsController(SucculentContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Posts/Create (顯示發文畫面)
        public IActionResult Create()
        {
            // 1. 取得目前登入者的帳號 (從 Cookie 抓)
            var currentAccount = User.Identity.Name;
            var currentMember = _context.Members.FirstOrDefault(m => m.Account == currentAccount);

            if (currentMember == null) return RedirectToAction("Login", "Members");

            // 2. 準備下拉選單資料給畫面 (ViewBag)
            // 🌟 字典下拉選單：讓使用者選品種 (顯示中文名稱，存入 SpeciesID)
            ViewBag.SpeciesID = new SelectList(_context.Species.OrderBy(s => s.SpeciesID), "SpeciesID", "CommonName");

            // 🌟 舊植物下拉選單：去 Plant 表找「屬於這個登入者」且「還沒被刪除」的植物
            var myPlants = _context.Plants
                .Where(p => p.MemberID == currentMember.MemberID && !p.IsDeleted)
                .OrderByDescending(p => p.CreateDate)
                .ToList();
            ViewBag.ExistingPlantID = new SelectList(myPlants, "PlantID", "PlantNickname");

            return View(new CreatePostVM()); // 把空的快遞箱送到畫面上
        }

        // POST: Posts/Create (接收發文資料，超級秘書開始工作！)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePostVM vm)
        {
            // 取得目前登入者
            var currentAccount = User.Identity.Name;
            var currentMember = _context.Members.FirstOrDefault(m => m.Account == currentAccount);
            if (currentMember == null) return RedirectToAction("Login", "Members");

            if (ModelState.IsValid)
            {
                long? finalPlantId = null; // 用來記錄最後要綁定給貼文的植物編號

                // ====================================================
                // 🌱 神級分流邏輯：處理「植物建檔」
                // ====================================================
                // 只有當發文類別是 "2" (日誌) 或 "3" (心得) 時，才處理植物綁定
                if (vm.PostType == "2" || vm.PostType == "3")
                {
                    if (vm.IsNewPlant)
                    {
                        // 情況 A：使用者說這是「新買的植物」 -> 幫他建檔！
                        var newPlant = new Plant
                        {
                            MemberID = currentMember.MemberID,
                            SpeciesID = vm.SpeciesID,
                            PlantNickname = string.IsNullOrWhiteSpace(vm.PlantNickname) ? "未命名小肉肉" : vm.PlantNickname,
                            CreateDate = DateTime.Now,
                            PlantImage = "default.jpg" // 預設植物大頭貼
                        };
                        _context.Plants.Add(newPlant);
                        await _context.SaveChangesAsync(); // 🌟 存檔！此時資料庫會自動配發全新的 PlantID

                        finalPlantId = newPlant.PlantID; // 把熱騰騰的新編號抄下來
                    }
                    else
                    {
                        // 情況 B：使用者選擇「現有植物」 -> 直接拿他選的 ID
                        finalPlantId = vm.ExistingPlantID;
                    }
                }

                // ====================================================
                // 📸 處理圖片上傳 (引入 ImageSharp 轉檔魔法)
                // ====================================================
                string finalPostImage = null;
                if (vm.UploadImage != null && vm.UploadImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "posts");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid().ToString() + ".jpg";
                    string path = Path.Combine(uploadsFolder, fileName);

                    using (var image = await Image.LoadAsync(vm.UploadImage.OpenReadStream()))
                    {
                        await image.SaveAsJpegAsync(path);
                    }
                    finalPostImage = fileName;
                }

                // ====================================================
                // 📝 建立「多肉日誌 (Post)」本體
                // ====================================================
                var newPost = new Post
                {
                    MemberID = currentMember.MemberID,
                    PlantID = finalPlantId, // 🌟 把剛剛算好的植物編號綁定上去 (如果是純提問，這裡就會是 Null)
                    PostType = vm.PostType,
                    PostTitle = vm.PostTitle,
                    PostContent = vm.PostContent,
                    PostImage = finalPostImage,
                    IsPublic = vm.IsPublic,
                    CreatedTime = DateTime.Now,
                    LikeCount = 0,
                    IsDeleted = false
                };

                _context.Posts.Add(newPost);
                await _context.SaveChangesAsync(); // 🌟 最終存檔！無縫建檔完美成功！

                // 🌟 新增：發布成功的 Toast 訊息
                TempData["SuccessMessage"] = "🎉 貼文發佈成功！快來看看大家的迴響吧！";

                // 先導向首頁，之後我們再建置日誌牆列表
                return RedirectToAction("Index", "Home");
            }

            // 🚨 如果防呆驗證失敗，要把下拉選單再準備一次還給畫面，不然網頁會壞掉
            ViewBag.SpeciesID = new SelectList(_context.Species.OrderBy(s => s.SpeciesID), "SpeciesID", "CommonName", vm.SpeciesID);
            var myPlantsFail = _context.Plants.Where(p => p.MemberID == currentMember.MemberID && !p.IsDeleted).ToList();
            ViewBag.ExistingPlantID = new SelectList(myPlantsFail, "PlantID", "PlantNickname", vm.ExistingPlantID);

            return View(vm);
        }

        // ====================================================
        // 📖 我的多肉日誌 (專屬手帳本)
        // ====================================================
        public async Task<IActionResult> MyDiary()
        {
            // 1. 確認現在是哪位園丁
            var currentAccount = User.Identity.Name;
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);

            if (currentMember == null) return RedirectToAction("Login", "Members");

            // 2. 去資料庫把「屬於這位園丁」且「沒被刪除」的所有貼文抓出來！
            // 🌟 注意：這裡不加 p.IsPublic == "1" 的條件，因為是自己的日誌，私人公開都要看得到！
            var myPosts = await _context.Posts
                .Include(p => p.Member)
                .Include(p => p.Plant)
                .Include(p => p.Favorites).ThenInclude(f => f.Member)
                .Where(p => p.MemberID == currentMember.MemberID && p.IsDeleted == false)
                .OrderByDescending(p => p.CreatedTime)
                .ToListAsync();

            return View(myPosts); // 把專屬日記本送給畫面
        }

        // GET: Posts/Details/5 (閱讀單篇貼文全文)
        [AllowAnonymous] // 🌟 允許沒登入的路人也可以看文章，吸引他們註冊！
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.Member) // 發文者資料
                .Include(p => p.Plant)  // 植物標籤
                                        // 🌟 升級 1：把這篇文「沒有被刪除」的留言通通抓出來，並且連同「留言者的會員資料」一起打包！
                .Include(p => p.Comments.Where(c => c.IsDeleted == false))
                    .ThenInclude(c => c.Member)
                // 🌟 升級 2：把按讚紀錄抓出來，這樣我們才知道有幾個人按讚
                .Include(p => p.PostLikes)
                .FirstOrDefaultAsync(m => m.PostID == id);

            if (post == null)
            {
                return NotFound();
            }

            // ==========================================
            // 🌟 新增：上一篇 / 下一篇 導航邏輯
            // ==========================================
            // 1. 取得「上一篇」(較新的貼文，也就是 ID 大於目前的最小值)
            var prevPost = await _context.Posts
                .Where(p => p.PostID > id && p.IsDeleted == false && p.IsPublic == "1")
                .OrderBy(p => p.PostID)
                .FirstOrDefaultAsync();
            ViewBag.PrevId = prevPost?.PostID; // 將 ID 打包帶給畫面

            // 2. 取得「下一篇」(較舊的貼文，也就是 ID 小於目前的最大值)
            var nextPost = await _context.Posts
                .Where(p => p.PostID < id && p.IsDeleted == false && p.IsPublic == "1")
                .OrderByDescending(p => p.PostID)
                .FirstOrDefaultAsync();
            ViewBag.NextId = nextPost?.PostID; // 將 ID 打包帶給畫面

            // 🌟 貼心小判斷：檢查目前登入的園丁，是不是已經按過讚或收藏了？
            if (User.Identity.IsAuthenticated)
            {
                var currentAccount = User.Identity.Name;
                var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);
                if (currentMember != null)
                {
                    // 檢查按讚
                    ViewData["IsLikedByMe"] = post.PostLikes.Any(l => l.MemberID == currentMember.MemberID);

                    // 🌟 檢查收藏 (假設你的資料表叫做 Favorites)
                    ViewData["IsFavoritedByMe"] = await _context.Favorites.AnyAsync(f => f.PostID == id && f.MemberID == currentMember.MemberID);
                }
            }

            return View(post); // 把完整的貼文包裹送到專屬畫面上
        }

        // ====================================================
        // 💬 1. 新增留言 (無痕 AJAX 版)
        // ====================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(long postId, string commentContent, IFormFile? uploadImage)
        {
            // 防呆：如果沒寫字就按送出，回傳 JSON 錯誤
            if (string.IsNullOrWhiteSpace(commentContent)) return Json(new { success = false, message = "留言內容不能空白喔！" });

            // 找出目前是哪位園丁在留言
            var currentAccount = User.Identity.Name;
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);
            if (currentMember == null) return Json(new { success = false, message = "請先登入才能留言喔！" });

            string finalImageName = null;

            // 📸 處理留言的圖片上傳
            if (uploadImage != null && uploadImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "comments");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                finalImageName = Guid.NewGuid().ToString() + ".jpg";
                string path = Path.Combine(uploadsFolder, finalImageName);

                using (var image = await Image.LoadAsync(uploadImage.OpenReadStream()))
                {
                    // 稍微壓縮一下留言圖片，避免佔用太多空間
                    image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(800, 800) }));
                    await image.SaveAsJpegAsync(path);
                }
            }

            // 建立新的留言包裹
            var newComment = new Comment
            {
                PostID = postId,
                MemberID = currentMember.MemberID,
                CommentContent = commentContent,
                CommentImage = finalImageName,
                CommentTime = DateTime.Now,
                IsDeleted = false
            };

            // 存進資料庫
            _context.Comments.Add(newComment);
            await _context.SaveChangesAsync();

            // 🌟 改變囉！不跳轉網頁，而是打包 JSON 送回給前端，讓 JavaScript 自己把留言畫出來
            return Json(new
            {
                success = true,
                message = "留言發佈成功！",
                commentId = newComment.CommentID,
                content = newComment.CommentContent,
                imageUrl = newComment.CommentImage,
                time = newComment.CommentTime.ToString("yyyy/MM/dd HH:mm"),
                nickname = currentMember.Nickname,
                account = currentMember.Account,
                avatar = string.IsNullOrEmpty(currentMember.Avatar) ? "default.jpg" : currentMember.Avatar
            });
        }

        // ====================================================
        // ✏️ 2. 編輯留言 (無痕 AJAX 支援換圖與刪圖版)
        // ====================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(long commentId, long postId, string editContent, IFormFile? editUploadImage, string removeImageFlag)
        {
            if (string.IsNullOrWhiteSpace(editContent)) return Json(new { success = false, message = "修改的留言內容不能空白喔！" });

            // 把留言跟留言者的資料一起抓出來
            var comment = await _context.Comments.Include(c => c.Member).FirstOrDefaultAsync(c => c.CommentID == commentId);

            // 🚨 資安防護：找不到留言，或是目前登入的人不是留言的主人，就擋下來！
            if (comment == null || comment.Member.Account != User.Identity.Name) return Json(new { success = false, message = "發生錯誤或權限不足！" });

            comment.CommentContent = editContent;

            // 🌟 處理圖片邏輯
            if (removeImageFlag == "Y")
            {
                comment.CommentImage = null; // 園丁選擇把照片刪除
            }
            else if (editUploadImage != null && editUploadImage.Length > 0)
            {
                // 園丁上傳了新照片，覆蓋原本的
                var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "comments");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string finalImageName = Guid.NewGuid().ToString() + ".jpg";
                string path = Path.Combine(uploadsFolder, finalImageName);

                using (var image = await Image.LoadAsync(editUploadImage.OpenReadStream()))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(800, 800) }));
                    await image.SaveAsJpegAsync(path);
                }
                comment.CommentImage = finalImageName;
            }

            _context.Update(comment);
            await _context.SaveChangesAsync();

            // 🌟 編輯成功，回傳 JSON 更新畫面
            return Json(new
            {
                success = true,
                message = "留言修改成功！",
                commentId = comment.CommentID,
                content = comment.CommentContent,
                imageUrl = comment.CommentImage
            });
        }

        // ====================================================
        // 🗑️ 3. 刪除留言 (無痕 AJAX 軟刪除版)
        // ====================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(long commentId, long postId)
        {
            var comment = await _context.Comments.Include(c => c.Member).FirstOrDefaultAsync(c => c.CommentID == commentId);

            // 🚨 資安防護
            if (comment == null || comment.Member.Account != User.Identity.Name) return Json(new { success = false, message = "發生錯誤或權限不足！" });

            comment.IsDeleted = true; // 🌟 軟刪除
            _context.Update(comment);
            await _context.SaveChangesAsync();

            // 🌟 刪除成功，讓前端 JS 把那則留言收合掉
            return Json(new { success = true, message = "留言已刪除！" });
        }

        // ====================================================
        // ❤️ 按讚 / 收回讚 功能 (提供給前端 AJAX 無刷新呼叫)
        // ====================================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleLike(long postId)
        {
            var currentAccount = User.Identity.Name;
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);

            if (currentMember == null)
            {
                return Json(new { success = false, message = "請先登入才能按讚喔！" });
            }

            // 去找找看這位園丁是不是已經對這篇文按過讚了？
            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostID == postId && l.MemberID == currentMember.MemberID);

            bool isLikedNow = false;

            if (existingLike != null)
            {
                // 已經按過了 ➡️ 執行「收回讚 (移除紀錄)」
                _context.PostLikes.Remove(existingLike);
                isLikedNow = false;
            }
            else
            {
                // 還沒按過 ➡️ 執行「新增讚 (加入紀錄)」
                var newLike = new PostLike
                {
                    PostID = postId,
                    MemberID = currentMember.MemberID
                };
                _context.PostLikes.Add(newLike);
                isLikedNow = true;
            }

            await _context.SaveChangesAsync();

            // 重新計算這篇文目前總共有幾個讚
            var totalLikes = await _context.PostLikes.CountAsync(l => l.PostID == postId);

            // 把最新狀態回傳給前端畫面
            return Json(new { success = true, isLiked = isLikedNow, likeCount = totalLikes });
        }

        // ====================================================
        // 🔖 收藏 / 取消收藏 功能 (提供給前端 AJAX 無刷新呼叫)
        // ====================================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(long postId)
        {
            try
            {
                var currentAccount = User.Identity.Name;
                var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);

                if (currentMember == null) return Json(new { success = false, message = "請先登入才能收藏喔！" });

                // 去找找看這位園丁是不是已經收藏過這篇文了？
                var existingFav = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.PostID == postId && f.MemberID == currentMember.MemberID);

                bool isFavoritedNow = false;

                if (existingFav != null)
                {
                    // 已經收藏過了 ➡️ 取消收藏 (移除紀錄)
                    _context.Favorites.Remove(existingFav);
                    isFavoritedNow = false;
                }
                else
                {
                    // 還沒收藏過 ➡️ 加入收藏 (加入紀錄)
                    var newFav = new Favorite
                    {
                        PostID = postId,
                        MemberID = currentMember.MemberID,
                        FavType = "Post" // 🌟 破案關鍵：明確告訴資料庫這是一篇「貼文」的收藏！
                    };
                    _context.Favorites.Add(newFav);
                    isFavoritedNow = true;
                }

                await _context.SaveChangesAsync();

                // 回傳最新狀態給前端，讓按鈕變色
                return Json(new { success = true, isFavorited = isFavoritedNow });
            }
            catch (Exception ex)
            {
                // 🚨 捕捉隱形的伺服器錯誤並回傳給畫面
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "資料庫存檔發生錯誤：" + errorMsg });
            }
        }

        // ====================================================
        // 🔖 我的專屬收藏夾 (MyFavorites)
        // ====================================================
        [Authorize]
        public async Task<IActionResult> MyFavorites()
        {
            // 1. 確認現在是哪位園丁
            var currentAccount = User.Identity.Name;
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);

            if (currentMember == null) return RedirectToAction("Login", "Members");

            // ==========================================
            // 🌟 新增這段：去資料庫把這個人收藏的「圖鑑」撈出來
            // ==========================================
            var favoriteSpecies = await _context.Favorites
                .Include(f => f.Species)
                .Where(f => f.MemberID == currentMember.MemberID && f.FavType == "Species")
                .Select(f => f.Species) // 我們只需要畫面顯示 Species 的資料
                .ToListAsync();

            ViewBag.FavoriteSpecies = favoriteSpecies; // 裝進 ViewBag 帶去前端

            // 2. 去 Favorites 表把屬於這個人的貼文收藏找出來，並且把對應的 Post 資料一起「打包帶走」
            var myFavorites = await _context.Favorites
                // 🌟 透過 Favorite 關聯到 Post，再往下抓發文者、植物、按讚與留言數
                .Include(f => f.Post)
                    .ThenInclude(p => p.Member)
                .Include(f => f.Post)
                    .ThenInclude(p => p.Plant)
                .Include(f => f.Post)
                    .ThenInclude(p => p.PostLikes)
                .Include(f => f.Post)
                    .ThenInclude(p => p.Comments.Where(c => c.IsDeleted == false))
                // 🌟 條件：是這位園丁收藏的 + 類型是貼文 + 貼文本身還沒被作者刪除
                .Where(f => f.MemberID == currentMember.MemberID && f.FavType == "Post" && f.Post.IsDeleted == false)
                .OrderByDescending(f => f.FavoriteID) // 依照收藏的先後順序（最新的在最上面）
                .Select(f => f.Post) // 🌟 關鍵魔法：畫面只需要 Post 的資料，所以我們把它單獨抽出來傳給畫面
                .ToListAsync();

            return View(myFavorites);
        }

        // ====================================================
        // ✏️ 編輯貼文功能
        // ====================================================

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts.Include(p => p.Member).FirstOrDefaultAsync(p => p.PostID == id && !p.IsDeleted);
            if (post == null) return NotFound();

            // 🚨 權限防護：如果現在登入的帳號，不是這篇貼文的作者，就踢回首頁！
            if (post.Member.Account != User.Identity.Name) return RedirectToAction("Index", "Home");

            // 🌟 準備下拉選單：把這個園丁溫室裡「所有還活著的植物」找出來讓他選
            var myPlants = _context.Plants
                .Where(p => p.MemberID == post.MemberID && !p.IsDeleted)
                .OrderByDescending(p => p.CreateDate)
                .ToList();
            ViewBag.PlantID = new SelectList(myPlants, "PlantID", "PlantNickname", post.PlantID);

            var vm = new EditPostVM
            {
                PostID = post.PostID,
                PostType = post.PostType, // 帶入原本的類別
                PlantID = post.PlantID,   // 帶入原本的植物
                PostTitle = post.PostTitle,
                PostContent = post.PostContent,
                IsPublic = post.IsPublic,
                ExistingImage = post.PostImage
            };

            return View(vm);
        }

        // POST: Posts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EditPostVM vm)
        {
            if (id != vm.PostID) return NotFound();

            var post = await _context.Posts.Include(p => p.Member).FirstOrDefaultAsync(p => p.PostID == id && !p.IsDeleted);
            if (post == null) return NotFound();

            if (post.Member.Account != User.Identity.Name) return RedirectToAction("Index", "Home");

            if (ModelState.IsValid)
            {
                // 🌟 更新類別與植物邏輯
                post.PostType = vm.PostType;
                // 如果改成了「舉手發問(1)」，就把植物綁定清空；否則就綁定他選的植物
                post.PlantID = (vm.PostType == "1") ? null : vm.PlantID;

                post.PostTitle = vm.PostTitle;
                post.PostContent = vm.PostContent;
                post.IsPublic = vm.IsPublic;
                post.UpdatedTime = DateTime.Now;

                if (vm.UploadImage != null && vm.UploadImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "posts");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    string fileName = Guid.NewGuid().ToString() + ".jpg";
                    string path = Path.Combine(uploadsFolder, fileName);
                    using (var image = await Image.LoadAsync(vm.UploadImage.OpenReadStream()))
                    {
                        await image.SaveAsJpegAsync(path);
                    }
                    post.PostImage = fileName;
                }

                _context.Update(post);
                await _context.SaveChangesAsync();

                // 🌟 新增：修改成功的 Toast 訊息
                TempData["SuccessMessage"] = "✨ 貼文修改成功！";

                return RedirectToAction("Details", new { id = post.PostID });
            }

            // 如果驗證失敗，要把下拉選單再傳一次，畫面才不會壞掉
            var myPlantsFail = _context.Plants.Where(p => p.MemberID == post.MemberID && !p.IsDeleted).ToList();
            ViewBag.PlantID = new SelectList(myPlantsFail, "PlantID", "PlantNickname", vm.PlantID);
            return View(vm);
        }

        // ====================================================
        // 📌 設為置頂 / 取消置頂 (無痕 AJAX，限真正 IsAdmin 的管理員！)
        // ====================================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> TogglePin(long postId)
        {
            var currentAccount = User.Identity.Name;
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);

            // 🚨 終極防護：找不到人，或是 IsAdmin 不是 true，直接擋掉！
            if (currentMember == null || !currentMember.IsAdmin)
            {
                return Json(new { success = false, message = "權限不足，只有管理員可以執行置頂操作！" });
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostID == postId);
            if (post == null) return Json(new { success = false, message = "找不到這篇貼文！" });

            post.IsPinned = (post.IsPinned == "1") ? "0" : "1";

            _context.Update(post);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isPinned = post.IsPinned == "1",
                message = post.IsPinned == "1" ? "📌 已設為置頂！" : "✅ 已取消置頂！"
            });
        }

        // ====================================================
        // 🗑️ 刪除貼文功能 (發文者本人 或 管理員 皆可執行)
        // ====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(long id)
        {
            var currentAccount = User.Identity.Name;
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == currentAccount);
            if (currentMember == null) return RedirectToAction("Login", "Members");

            var post = await _context.Posts.Include(p => p.Member).FirstOrDefaultAsync(p => p.PostID == id);

            if (post != null)
            {
                // 🌟 核心防護：是發文者本人？或者是管理員？兩者皆可放行！
                if (post.Member.Account == currentAccount || currentMember.IsAdmin)
                {
                    post.IsDeleted = true; // 軟刪除
                    _context.Update(post);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "🗑️ 貼文已成功刪除！";
                }
                else
                {
                    TempData["ErrorMessage"] = "⛔ 權限不足！無法刪除他人的貼文。";
                }
            }

            // 🌟 聰明導航：判斷管理員是從「後台」還是「前台」按刪除的？
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && referer.Contains("AdminIndex"))
            {
                return RedirectToAction(nameof(AdminIndex)); // 回到後台列表
            }

            return RedirectToAction("Index", "Home"); // 回到首頁
        }

        // ====================================================
        // 🛡️ 後台管理區：貼文列表 (僅限管理員)
        // ====================================================
        [Authorize]
        // 🌟 新增 int? page 參數
        public async Task<IActionResult> AdminIndex(string searchString, int? page)
        {
            // 🚨 後台門禁檢查
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin)
            {
                TempData["ErrorMessage"] = "⛔ 權限不足！只有系統管理員可以進入貼文後台喔！";
                return RedirectToAction("Index", "Home");
            }

            ViewData["CurrentFilter"] = searchString;

            // 抓取所有「未刪除」的貼文，並打包發文者與植物資料
            var posts = _context.Posts
                .Include(p => p.Member)
                .Include(p => p.Plant)
                .Where(p => p.IsDeleted == false);

            // 關鍵字搜尋 (可搜標題或發文者暱稱)
            if (!String.IsNullOrEmpty(searchString))
            {
                posts = posts.Where(s => s.PostTitle.Contains(searchString) || s.Member.Nickname.Contains(searchString));
            }

            // 🌟 設定分頁規則
            int pageNumber = page ?? 1;
            int pageSize = 10; // 🌟 貼文後台列表一頁顯示 10 筆

            // 🌟 依照時間「由新到舊」排序，並使用 ToPagedList 輸出分頁資料
            return View(posts.OrderByDescending(p => p.CreatedTime).ToPagedList(pageNumber, pageSize));
        }

        // ====================================================
        // 🗑️ 後台專屬：一鍵批次刪除多篇貼文
        // ====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteMultiple(List<long> postIds)
        {
            // 1. 🚨 嚴格的後台門禁檢查
            var currentMember = await _context.Members.FirstOrDefaultAsync(m => m.Account == User.Identity.Name);
            if (currentMember == null || !currentMember.IsAdmin)
            {
                TempData["ErrorMessage"] = "⛔ 權限不足！只有系統管理員可以執行此操作。";
                return RedirectToAction("Index", "Home");
            }

            // 2. 防呆：如果管理員沒勾選任何東西就按送出
            if (postIds == null || !postIds.Any())
            {
                TempData["ErrorMessage"] = "⚠️ 請先勾選想要刪除的貼文喔！";
                return RedirectToAction(nameof(AdminIndex));
            }

            // 3. 找出所有被勾選的貼文
            var postsToDelete = await _context.Posts
                .Where(p => postIds.Contains(p.PostID))
                .ToListAsync();

            // 4. 把它們全部標記為「已刪除」
            foreach (var post in postsToDelete)
            {
                post.IsDeleted = true;
                _context.Update(post);
            }

            // 5. 一口氣儲存變更！
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"🗑️ 太有效率了！成功強制刪除了 {postsToDelete.Count} 篇貼文！";
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}