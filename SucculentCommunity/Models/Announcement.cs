using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SucculentCommunity.Models
{
    [Table("Announcement")] // 強制告訴 EF Core，去 SQL Server 建表時請用單數 Announcement
    public class Announcement
    {
        // 公告序號 (P.K, bigint -> long, identity)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnnounceID { get; set; }

        // 標題 (nvarchar(100), 不允許空值)
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        // 內容 (nvarchar(max), 不允許空值)
        // 不加 StringLength，EF Core 會自動開到最大，讓管理員盡情發布長篇大論！
        [Required]
        public string Content { get; set; } = null!;

        // 發布時間 (datetime, 預設系統時間)
        // 完美對應你設計的 default getdate()
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime PostTime { get; set; } = DateTime.Now;
    }
}