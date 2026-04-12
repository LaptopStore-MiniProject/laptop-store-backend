using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Services.DTOs.Product
{
    public class ProductRequestDto
    {
        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [MaxLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
        public int StockQuantity { get; set; }

        public string Cpu { get; set; } = string.Empty;
        public string Ram { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
        public string ScreenSize { get; set; } = string.Empty;
        public string Vga { get; set; } = string.Empty;

        [Required(ErrorMessage = "BrandId không được để trống")]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "CategoryId không được để trống")]
        public int CategoryId { get; set; }
    }
}
