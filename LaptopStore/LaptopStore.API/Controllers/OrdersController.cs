using LaptopStore.API.Common;
using LaptopStore.Repositories.Entities;
using LaptopStore.Services.DTOs.Order;
using LaptopStore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaptopStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] OrderCreateRequestDto dto) 
        {
            try
            {
                Guid userId = GetCurrentUserId();
                _logger.LogInformation("[OrdersController] : Nhận request checkout của user {UserId}.", userId);

                CheckoutOrderResultDto result = await _orderService.CheckoutAsync(userId,dto);

                if (!result.IsSuccess) 
                {
                    return BadRequest(new ApiResponse<CheckoutOrderResultDto>
                    {
                        Status = 400,
                        Message = result.Message,
                        Data = result
                    });
                }
                return Ok(new ApiResponse<CheckoutOrderResultDto>
                {
                    Status = 200,
                    Message = "Đặt hàng thành công.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OrdersController] : Lỗi hệ thống khi checkout.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "Lỗi máy chủ.",
                    Data = null
                });
            }
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders() 
        {
            try
            {
                Guid userId = GetCurrentUserId();
                _logger.LogInformation("[OrdersController] : Nhận request lấy danh sách đơn hàng của user {UserId}.", userId);
                
                List<OrderResponseDto> orders = await _orderService.GetMyOrderAsync(userId);

                return Ok(new ApiResponse<List<OrderResponseDto>>
                {
                    Status = 200,
                    Message = "Lấy danh sách đơn hàng thành công.",
                    Data = orders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OrdersController] : Lỗi hệ thống khi lấy danh sách đơn hàng của user.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "Lỗi máy chủ.",
                    Data = null
                });
            }
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId) 
        {
            try
            {
                Guid userId = GetCurrentUserId();
                bool isManager = User.IsInRole("Manager");
                _logger.LogInformation("[OrdersController] : Nhận request lấy chi tiết đơn hàng OrderId = {OrderId}.", orderId);
                OrderResponseDto order = await _orderService.GetOrderByIdAsync(userId,orderId, isManager);
                if (order == null) 
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Status = 404,
                        Message = "Không tìm thấy đơn hàng.",
                        Data = null
                    });
                }
                return Ok(new ApiResponse<OrderResponseDto>
                {
                    Status = 200,
                    Message = "Lấy chi tiết đơn hàng thành công.",
                    Data = order
                });
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "[OrdersController] : Lỗi hệ thống khi lấy chi tiết đơn hàng.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "Lỗi máy chủ.",
                    Data = null
                });
            }
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("manager/all")]
        public async Task<IActionResult> GetAllOrders() 
        {
            try
            {
                _logger.LogInformation("[OrdersController] : Admin yêu cầu lấy toàn bộ đơn hàng.");
                List<OrderResponseDto> orders = await _orderService.GetAllOrdersAsync();
                return Ok(new ApiResponse<List<OrderResponseDto>>
                {
                    Status = 200,
                    Message = "Lấy toàn bộ đơn hàng thành công.",
                    Data = orders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OrdersController] : Lỗi hệ thống khi manager lấy toàn bộ đơn hàng.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "Lỗi máy chủ.",
                    Data = null
                });
            }
        }

        [Authorize(Roles = "Manager")]
        [HttpPut("{orderId}/status")]
        public async Task<IActionResult> UpdateStatus(Guid orderId, [FromBody] OrderStatusUpdateRequestDto dto) 
        {
            try
            {
                _logger.LogInformation("[OrdersController] : Admin yêu cầu cập nhật trạng thái đơn hàng OrderId = {OrderId}.", orderId);
                bool isSuccess = await _orderService.UpdateOrderStatusAsync(orderId, dto.Status);
                if (!isSuccess) 
                {
                    return BadRequest(new ApiResponse<object> 
                    {
                        Status = 400,
                        Message = "Không thể cập nhật trạng thái đơn hàng. Có thể đơn hàng không tồn tại hoặc trạng thái không hợp lệ.",
                        Data = null,
                    });
                }
                return Ok(new ApiResponse<object> 
                {
                    Status = 200,
                    Message = "Cập nhật trạng thái đơn hàng thành công.",
                    Data = null,
                });
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "[OrdersController] : Lỗi hệ thống khi cập nhật trạng thái đơn hàng.");
                return StatusCode(500, new ApiResponse<object> 
                {
                    Status = 500,
                    Message = "Lỗi máy chủ.",
                    Data = null
                });
            }
        }

        private Guid GetCurrentUserId() 
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                throw new UnauthorizedAccessException("Token không hợp lệ hoặc không chứa UserId.");
            }
            return userId;
        }
    }
}
