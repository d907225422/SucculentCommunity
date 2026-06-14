using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SucculentCommunity.Models
{
    [Table("PostLike")] // 強制告訴 EF Core，去 SQL Server 建表時請用單數 PostLike
    // 類別名稱升級為 PostLike
    public class PostLike
    {
        // 按讚序號 (配合類別名稱，改成 PostLikeID 會更專業)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PostLikeID { get; set; }

        // ---------------------------------------------------
        // 外鍵區塊 1：對應 Post 表 (按了哪篇貼文讚)
        // ---------------------------------------------------
        [Required]
        public long PostID { get; set; }

        [ForeignKey("PostID")]
        public virtual Post Post { get; set; } = null!;

        // ---------------------------------------------------
        // 外鍵區塊 2：對應 Member 表 (是誰按的讚)
        // ---------------------------------------------------
        [Required]
        [StringLength(5)]
        [Column(TypeName = "nchar(5)")]
        public string MemberID { get; set; } = null!;

        [ForeignKey("MemberID")]
        public virtual Member Member { get; set; } = null!;
    }
}