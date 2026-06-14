using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SucculentCommunity.Models
{
    [Table("Plant")] // 強制告訴 EF Core，去 SQL Server 建表時請用單數 Plant
    public class Plant
    {
        // 檔案序號 (P.K, bigint, identity)
        // 在 C# 中，bigint 對應的型別是 long
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PlantID { get; set; }

        // ---------------------------------------------------
        // 外鍵區塊 1：對應 Member 表 (不允許空值)
        // ---------------------------------------------------

        // 實體欄位
        [Required]
        [StringLength(5)]
        [Column(TypeName = "nchar(5)")]
        public string MemberID { get; set; } = null!;

        // 導覽屬性：告訴 EF Core 這個 MemberID 是連到 Member 表
        [ForeignKey("MemberID")]
        public virtual Member Member { get; set; } = null!;

        // ---------------------------------------------------
        // 外鍵區塊 2：對應 Species 表 (允許空值 isNull = Y)
        // ---------------------------------------------------

        // 實體欄位 (注意 string 後面的問號)
        [StringLength(5)]
        [Column(TypeName = "nchar(5)")]
        public string? SpeciesID { get; set; }

        // 導覽屬性：因為 SpeciesID 允許 Null，所以對應的 Species 物件也要加問號
        [ForeignKey("SpeciesID")]
        public virtual Species? Species { get; set; }

        // ---------------------------------------------------

        // 自訂暱稱 (nvarchar(50), 不允許空值)
        [Required]
        [StringLength(50)]
        public string PlantNickname { get; set; } = null!;

        // 建檔日期 (預設系統時間)
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreateDate { get; set; } = DateTime.Now;

        // 植物圖片 (nvarchar(255), 不允許空值, 有預設值)
        [Required]
        [StringLength(255)]
        [DefaultValue("default.jpg")]
        public string PlantImage { get; set; } = "default.jpg";

        // 🌟 軟刪除標記 (C# bool -> SQL bit, false=正常, true=已刪除)
        [Required]
        [DefaultValue(false)]
        public bool IsDeleted { get; set; } = false;
    }
}