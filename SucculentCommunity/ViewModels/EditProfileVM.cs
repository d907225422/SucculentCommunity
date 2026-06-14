using System.ComponentModel.DataAnnotations;

namespace SucculentCommunity.ViewModels
{
    public class EditProfileVM
    {
        // 🌟 新增：唯讀的帳號欄位
        [Display(Name = "會員帳號")]
        public string? Account { get; set; }

        [Display(Name = "園丁暱稱")]
        [Required(ErrorMessage = "暱稱不能空白喔！")]
        [StringLength(50)]
        public string Nickname { get; set; } = null!;

        public string? ExistingAvatar { get; set; }

        // 🌟 改為接收裁切後的 Base64 圖片字串
        public string? CroppedAvatar { get; set; }

        // 🌟 新增：驗證舊密碼
        [Display(Name = "舊密碼")]
        [DataType(DataType.Password)]
        public string? OldPassword { get; set; }

        [Display(Name = "新密碼 (若不修改請留空)")]
        [DataType(DataType.Password)]
        [StringLength(255)]
        //[StringLength(20, MinimumLength = 6, ErrorMessage = "密碼長度需在 6 到 20 個字元之間")]
        public string? NewPassword { get; set; }

        [Display(Name = "確認新密碼")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "兩次輸入的密碼不一致喔！")]
        public string? ConfirmNewPassword { get; set; }
    }
}