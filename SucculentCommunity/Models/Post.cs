using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SucculentCommunity.Models
{
    [Table("Post")] // 強制告訴 EF Core，去 SQL Server 建表時請用單數 Post
    public class Post
    {
        // 貼文序號 (P.K, bigint -> long, identity)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PostID { get; set; }

        // ---------------------------------------------------
        // 外鍵區塊 1：對應 Member 表 (發文者，不允許空值)
        // ---------------------------------------------------
        [Required]
        [StringLength(5)]
        [Column(TypeName = "nchar(5)")]
        public string MemberID { get; set; } = null!;

        [ForeignKey("MemberID")]
        public virtual Member Member { get; set; } = null!;

        // ---------------------------------------------------
        // 外鍵區塊 2：對應 Plant 表 (關聯植物，允許空值 isNull = Y)
        // ---------------------------------------------------
        // 注意：因為 PlantID 在資料庫是 bigint，這裡對應 long。
        // 允許 Null，所以要加上問號變成 long?
        public long? PlantID { get; set; }

        [ForeignKey("PlantID")]
        public virtual Plant? Plant { get; set; }

        // ---------------------------------------------------

        // 貼文類別 (nchar(1), 1=提問, 2=日誌, 3=心得)
        [Required]
        [StringLength(1)]
        [Column(TypeName = "nchar(1)")]
        public string PostType { get; set; } = null!;

        // 貼文標題 (nvarchar(100))
        [Required]
        [StringLength(100)]
        public string PostTitle { get; set; } = null!;

        // 貼文內容 (nvarchar(max)) -> 不加 StringLength 限制就會自動變成 max
        [Required]
        public string PostContent { get; set; } = null!;

        // 貼文圖片 (nvarchar(255), 允許空值)
        [StringLength(255)]
        public string? PostImage { get; set; }

        // 分享狀態 (nchar(1), '1'=公開, '0'=私人)
        [Required]
        [StringLength(1)]
        [Column(TypeName = "nchar(1)")]
        [DefaultValue("1")]
        public string IsPublic { get; set; } = "1"; // 預設為公開，前端沒傳值就自動帶 1

        // 置頂標籤 (nchar(1), '1'=置頂, '0'=一般)
        [Required]
        [StringLength(1)]
        [Column(TypeName = "nchar(1)")]
        [DefaultValue("0")]
        public string IsPinned { get; set; } = "0"; // 預設為不置頂，前端沒傳值就自動帶 0

        // 按讚計數 (int, 預設 0)
        // 這是預防 NULL + 1 陷阱的關鍵防線！
        [Required]
        [DefaultValue(0)]
        public int LikeCount { get; set; } = 0;

        // 發布時間 (預設系統時間)
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        // 🌟 新增：最後更新時間 (允許空值，有編輯過才會有值)
        public DateTime? UpdatedTime { get; set; }

        // 🌟 軟刪除標記 (C# bool -> SQL bit, false=正常, true=已刪除)
        [Required]
        [DefaultValue(false)]
        public bool IsDeleted { get; set; } = false;

        // ---------------------------------------------------
        // 🌟 社群互動的關聯集合 (讓 Entity Framework 幫我們自動抓取)
        // ---------------------------------------------------
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}