using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SucculentCommunity.Models
{
    [Table("Favorite")] // 強制告訴 EF Core，去 SQL Server 建表時請用單數 Favorite
    public class Favorite
    {
        // 收藏序號 (P.K, bigint -> long, identity)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FavoriteID { get; set; }

        // ---------------------------------------------------
        // 外鍵區塊 1：對應 Member 表 (是誰收藏的，不允許空值)
        // ---------------------------------------------------
        [Required]
        [StringLength(5)]
        [Column(TypeName = "nchar(5)")]
        public string MemberID { get; set; } = null!;

        [ForeignKey("MemberID")]
        public virtual Member Member { get; set; } = null!;

        // ---------------------------------------------------
        // 收藏類型 (nvarchar(20), 不允許空值)
        // 用來記錄這筆收藏是 'Post' (貼文) 還是 'Species' (圖鑑)
        // ---------------------------------------------------
        [Required]
        [StringLength(20)]
        public string FavType { get; set; } = null!;

        // ---------------------------------------------------
        // 外鍵區塊 2：對應 Post 表 (允許空值 isNull = Y)
        // ---------------------------------------------------
        // 如果收藏的是圖鑑，這個欄位就會是 Null
        public long? PostID { get; set; }

        [ForeignKey("PostID")]
        public virtual Post? Post { get; set; }

        // ---------------------------------------------------
        // 外鍵區塊 3：對應 Species 表 (允許空值 isNull = Y)
        // ---------------------------------------------------
        // 如果收藏的是貼文，這個欄位就會是 Null
        [StringLength(5)]
        [Column(TypeName = "nchar(5)")]
        public string? SpeciesID { get; set; }

        [ForeignKey("SpeciesID")]
        public virtual Species? Species { get; set; }
    }
}