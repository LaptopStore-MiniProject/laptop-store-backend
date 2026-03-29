using System;
using System.Collections.Generic;

namespace LaptopStore.Repositories.Entities
{
    // [OrderEntity] : Hóa đơn mua hàng. Bắt buộc dùng Guid để người dùng không đoán được mã đơn hàng của người khác.
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Cancelled

        // Khóa ngoại tới User
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}