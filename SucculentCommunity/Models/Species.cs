using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SucculentCommunity.Models
{
    public class Species
    {
        // 品種代碼 (P.K, nchar(5), 不允許空值)
        [Display(Name = "品種代碼")]
        [Key]
        [StringLength(5)]
        [Column(TypeName = "nchar(5)")]
        public string SpeciesID { get; set; } = null!;

        // 中文名稱 (nvarchar(100), 不允許空值)
        [Display(Name = "中文名稱")]
        [Required]
        [StringLength(100)]
        public string CommonName { get; set; } = null!;

        // ---------------------------------------------------
        // 以下為「允許空值 (isNull = Y)」的欄位，注意型別後面的問號 '?'
        // ---------------------------------------------------

        // 學名 (nvarchar(100), 允許空值)
        [Display(Name = "學名")]
        [StringLength(100)]
        public string? ScientificName { get; set; }

        // 科別 (nvarchar(50), 允許空值)
        [Display(Name = "科別")]
        [StringLength(50)]
        public string? Family { get; set; }

        // 屬別 (nvarchar(50), 允許空值)
        [Display(Name = "屬別")]
        [StringLength(50)]
        public string? Genus { get; set; }

        // 生長季節 (nvarchar(20), 允許空值)
        [Display(Name = "生長季節")]
        [StringLength(20)]
        public string? GrowthSeason { get; set; }

        // 光照指標 (nvarchar(50), 允許空值)
        [Display(Name = "光照指標")]
        [StringLength(50)]
        public string? LightGuide { get; set; }

        // 澆水指標 (nvarchar(50), 允許空值)
        [Display(Name = "澆水指標")]
        [StringLength(50)]
        public string? WaterGuide { get; set; }

        // 介質需求 (nvarchar(100), 允許空值)
        [Display(Name = "介質需求")]
        [StringLength(100)]
        public string? SoilMix { get; set; }

        // ---------------------------------------------------

        // 品種描述 (nvarchar(max), 不允許空值)
        [Display(Name = "品種描述")]
        [Required]
        public string Description { get; set; } = null!;

        // 範例圖片 (nvarchar(255), 不允許空值, 有預設值)
        [Display(Name = "圖鑑照片")]
        [Required]
        [StringLength(255)]
        [DefaultValue("default.jpg")]
        public string SpeciesImg { get; set; } = "default.jpg";
    }
}