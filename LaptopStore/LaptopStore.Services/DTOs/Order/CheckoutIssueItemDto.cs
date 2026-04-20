using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaptopStore.Services.DTOs.Order
{
    public class CheckoutIssueItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        // [CheckoutIssueItemDto] : Loại lỗi business để frontend có thể render đúng nhóm vấn đề.
        public string IssueType {  get; set; } = string.Empty ;

        // [CheckoutIssueItemDto] : Mô tả rõ ràng để người dùng biết item đang bị lỗi gì.
        public string Message { get; set; } = string.Empty;

        public int RequestedQuantity { get; set; }
        public int AvailableStock {  get; set; }

        public decimal CartUnitPrice { get; set; }
        public decimal CurrentPrice { get; set; }
    }
}
