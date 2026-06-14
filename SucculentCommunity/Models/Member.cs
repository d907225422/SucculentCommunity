using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SucculentCommunity.Models
{
    [Table("Member")] // 強制告訴 EF Core，去 SQL Server 建表時請用單數 Member
    public class Member
    {
        // 會員編號 (P.K, nchar(5))
        [Display(Name = "會員編號")]
        [Key]
        [StringLength(5)]
        [Column(TypeName = "nchar(5)")]
        public string MemberID { get; set; } = null!;

        // 帳號 (不允許空值, nvarchar(50))
        [Display(Name = "會員帳號")]
        [Required]
        [StringLength(50)]
        public string Account { get; set; } = null!;

        // 密碼 (SHA256, nvarchar(255))
        [Display(Name = "登入密碼")]
        [Required]
        [StringLength(255)]
        public string Password { get; set; } = null!;

        // 暱稱 (nvarchar(50))
        [Display(Name = "園丁暱稱")]
        [Required]
        [StringLength(50)]
        public string Nickname { get; set; } = null!;

        // 頭像 (nvarchar(255), 預設 'default.jpg')
        [Display(Name = "大頭貼")]
        [Required]
        [StringLength(255)]
        [DefaultValue("default.jpg")]
        public string Avatar { get; set; } = "default.jpg";

        // 狀態 (nchar(1), 預設 '1')
        [Display(Name = "帳號狀態")]
        [Required]
        [StringLength(1)]
        [Column(TypeName = "nchar(1)")]
        [DefaultValue("1")]
        public string Status { get; set; } = "1";

        // 註冊時間 (預設系統時間)
        [Display(Name = "註冊時間")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime RegisterDate { get; set; } = DateTime.Now;

        // 🌟 新增：是否為管理員 (預設為 false，只有被特別提拔的人才是)
        [Display(Name = "管理員權限")]
        [Required]
        [DefaultValue(false)]
        public bool IsAdmin { get; set; } = false;
    }
}