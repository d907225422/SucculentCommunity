using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SucculentCommunity.ViewModels
{
    public class EditPostVM
    {
        public long PostID { get; set; }

        // 🌟 新增：允許修改貼文類別
        [Display(Name = "貼文類別")]
        public string PostType { get; set; } = null!;

        // 🌟 新增：允許修改關聯的植物 (只限溫室裡現有的植物)
        [Display(Name = "關聯植物")]
        public long? PlantID { get; set; }

        [Display(Name = "標題")]
        [Required(ErrorMessage = "標題不能為空喔！")]
        [StringLength(100)]
        public string PostTitle { get; set; } = null!;

        [Display(Name = "內容")]
        [Required(ErrorMessage = "內容不能為空喔！")]
        public string PostContent { get; set; } = null!;

        public string IsPublic { get; set; } = "1";

        public string? ExistingImage { get; set; }

        [Display(Name = "更換美照")]
        public IFormFile? UploadImage { get; set; }
    }
}