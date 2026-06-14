using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace SucculentCommunity.ViewModels
{
    public class EditPlantVM
    {
        public long PlantID { get; set; }

        [Display(Name = "植物品種")]
        public string? SpeciesID { get; set; }

        [Display(Name = "植物暱稱")]
        [Required(ErrorMessage = "請幫小肉肉取個可愛的暱稱吧！")]
        [StringLength(50)]
        public string PlantNickname { get; set; } = null!;

        // 🌟 新增：解鎖收編日期的修改權限！
        [Display(Name = "收編日期")]
        [Required(ErrorMessage = "收編日期不能空白喔！")]
        [DataType(DataType.Date)] // 讓網頁自動顯示漂亮的日期選擇器
        public DateTime CreateDate { get; set; }

        public string? ExistingImage { get; set; }

        [Display(Name = "更換專屬大頭照")]
        public IFormFile? UploadImage { get; set; }

        // 🌟 新增：用來接收前端 Cropper.js 裁切後的 Base64 圖片字串
        public string? CroppedImage { get; set; }
    }
}