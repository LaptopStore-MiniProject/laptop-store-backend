using LaptopStore.Services.DTOs.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaptopStore.Services.Interfaces
{
    public interface IOrderService
    {
        Task<CheckoutOrderResultDto> CheckoutAsync(Guid userId, OrderCreateRequestDto dto);
        Task<List<OrderResponseDto>> GetMyOrderAsync(Guid userId);
        Task<OrderResponseDto> GetOrderByIdAsync(Guid userId, Guid orderId, bool isManager = false);
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<bool> UpdateOrderStatusAsync(Guid orderId, string status);
    }
}
