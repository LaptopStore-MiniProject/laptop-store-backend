using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaptopStore.Services.DTOs.Order
{
    public class OrderCreateRequestDto
    {
        // [OrderCreateRequestDto] : Địa chỉ giao hàng là bắt buộc vì đây là nơi hệ thống sẽ giao đơn tới.
        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống")]
        [MaxLength(500, ErrorMessage = "Địa chỉ giao hàng không được vượt quá 500 ký tự.")]
        public string ShippingAddress { get; set; } = string.Empty;

        // [OrderCreateRequestDto] : Số điện thoại là bắt buộc để liên hệ giao hàng hoặc xác nhận đơn.
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
