using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaptopStore.Services.DTOs.Order
{
    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // Kèm theo danh sách chi tiết đơn hàng
        public List<OrderDetailResponseDto> OrderDetails { get; set; } = new List<OrderDetailResponseDto>();
    }
}
