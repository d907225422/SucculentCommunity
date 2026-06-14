using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SucculentCommunity.ViewModels
{
    public class CreatePostVM
    {
        // ==========================================
        // 📝 給「貼文與日誌 (Post)」用的欄位
        // ==========================================
        [Display(Name = "貼文類別")]
        public string PostType { get; set; } = "2"; // 預設幫園丁選好 "2=日誌"

        [Display(Name = "標題")]
        [Required(ErrorMessage = "幫這篇貼文取個標題吧！")]
        [StringLength(100)]
        public string PostTitle { get; set; } = null!;

        [Display(Name = "內容")]
        [Required(ErrorMessage = "寫點什麼跟大家分享吧！")]
        public string PostContent { get; set; } = null!;

        [Display(Name = "上傳美照")]
        public IFormFile? UploadImage { get; set; } // 用來接實體圖片檔案

        public string IsPublic { get; set; } = "1"; // 預設公開 (1=公開, 0=私人)

        // ==========================================
        // 🌱 給「植物庫建檔 (Plant)」用的欄位
        // ==========================================

        // 🌟 核心魔法開關：用來判斷園丁是「新買了植物」還是「幫舊植物發文」？
        public bool IsNewPlant { get; set; } = true;

        // 選擇 A：如果是舊植物，只要傳 PlantID 過來就好
        [Display(Name = "選擇我的植物")]
        public long? ExistingPlantID { get; set; }

        // 選擇 B：如果是新植物，就要填寫以下品種跟暱稱！
        [Display(Name = "植物品種")]
        public string? SpeciesID { get; set; }

        [Display(Name = "植物暱稱")]
        [StringLength(50, ErrorMessage = "暱稱不能超過 50 個字喔！")]
        public string? PlantNickname { get; set; }
    }
}