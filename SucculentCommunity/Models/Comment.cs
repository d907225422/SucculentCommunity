using System;
using System.ComponentModel; // 記得確保有 using 這個才能用 DefaultValue
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SucculentCommunity.Models
{
    [Table("Comment")] // 強制告訴 EF Core，去 SQL Server 建表時請用單數 Comment
    public class Comment
    {
        // 留言序號 (P.K, bigint -> long, identity)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CommentID { get; set; }

        // ---------------------------------------------------
        // 外鍵區塊 1：對應 Post 表 (這則留言屬於哪篇貼文)
        // ---------------------------------------------------
        // 注意：PostID 是 bigint，所以這裡是 long。不允許空值。
        [Required]
        public long PostID { get; set; }

        [ForeignKey("PostID")]
        public virtual Post Post { get; set; } = null!;

        // ---------------------------------------------------
        // 外鍵區塊 2：對應 Member 表 (是誰留的言)
        // ---------------------------------------------------
        // 注意：MemberID 是 nchar(5)，所以這裡是 string。不允許空值。
        [Required]
        [StringLength(5)]
        [Column(TypeName = "nchar(5)")]
        public string MemberID { get; set; } = null!;

        [ForeignKey("MemberID")]
        public virtual Member Member { get; set; } = null!;

        // ---------------------------------------------------

        // 留言內容 (nvarchar(max), 不允許空值)
        [Required]
        public string CommentContent { get; set; } = null!;

        // 留言圖片 (nvarchar(255), 允許空值 isNull = Y)
        [StringLength(255)]
        public string? CommentImage { get; set; }

        // 留言時間 (預設系統時間)
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CommentTime { get; set; } = DateTime.Now;

        // 🌟 軟刪除標記 (C# bool -> SQL bit, false=正常, true=已刪除)
        [Required]
        [DefaultValue(false)]
        public bool IsDeleted { get; set; } = false;
    }
}