using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaptopStore.Services.DTOs.Order
{
    public class CheckoutOrderResultDto
    {
        // [CheckoutOrderResultDto] : Đánh dấu checkout thành công hay bị chặn bởi business validation.
        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;

        // [CheckoutOrderResultDto] : Nếu cart có đổi giá thì frontend phải yêu cầu user sync giá trước khi checkout lại.
        public bool HasPriceChanges { get; set; }

        // [CheckoutOrderResultDto] : Nếu cart có item vượt quá tồn kho thì phải sửa quantity trước khi checkout.
        public bool HasStockIssues { get; set; }

        // [CheckoutOrderResultDto] : Nếu cart rỗng thì không thể tạo order.
        public bool IsCartEmpty { get; set; }
        public List<CheckoutIssueItemDto> Issues { get; set; } = new List<CheckoutIssueItemDto>();
        // [CheckoutOrderResultDto] : Chỉ có giá trị khi checkout thành công.
        public OrderResponseDto? Order { get; set; }
    }
}
