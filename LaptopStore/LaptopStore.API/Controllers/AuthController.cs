using AutoMapper;
using Azure.Core;
using LaptopStore.API.Common;
using LaptopStore.Services.DTOs.Auth;
using LaptopStore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthController> _logger;


        public AuthController(IAuthService authService,IMapper mapper,ILogger<AuthController> logger)
        {
            _authService = authService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            try
            {
                _logger.LogInformation("[AuthController] : Nhận request đăng ký tài khoản với email {Email}.", dto.Email);

                var result = await _authService.RegisterAsync(dto);

                if (result == null) 
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Status = 400,
                        Message = "Email đã tồn tại hoặc đăng ký thất bại.",
                        Data = null
                    });
                }
                SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiredAtUtc);

                var clientResponse = _mapper.Map<ClientAuthResponseDto>(result);

                return Ok(new ApiResponse<ClientAuthResponseDto>
                {
                    Status = 200,
                    Message = "Đăng ký tài khoản thành công.",
                    Data = clientResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController] : Lỗi hệ thống khi đăng ký.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "Lỗi máy chủ.",
                    Data = null
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                _logger.LogInformation("[AuthController] : Nhận request đăng nhập với email {Email}.", dto.Email);
                // 1. Gọi service xử lý đăng nhập, trả về AccessToken và RefreshToken
                var result = await _authService.LoginAsync(dto);

                if (result == null)
                {
                    _logger.LogWarning("[AuthController] : Đăng nhập thất bại với email {Email}", dto.Email);
                    return Unauthorized(new ApiResponse<object>
                    {
                        Status = 401,
                        Message = "Tài khoản hoặc mật khẩu không chính xác.",
                        Data = null
                    });
                }

                // Gắn Refresh Token vào HttpOnly Cookie
                SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiredAtUtc);

                // Cắt bỏ RefreshToken khỏi JSON Response để Client không lưu bậy bạ
                var clientResponse = _mapper.Map<ClientAuthResponseDto>(result);

                _logger.LogInformation("[AuthController] : Đăng nhập thành công và đã set Refresh Token Cookie cho user {UserId}", result.UserId);
                return Ok(new ApiResponse<ClientAuthResponseDto>
                {
                    Status = 200,
                    Message = "Đăng nhập thành công.",
                    Data = clientResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController] : Lỗi hệ thống khi đăng nhập.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "Lỗi máy chủ.",
                    Data = null
                });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                _logger.LogInformation("[AuthController] : Nhận request refresh token.");

                // Đọc token từ HttpOnly Cookie
                var refreshToken = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Status = 401,
                        Message = "Refresh token không hợp lệ hoặc đã hết hạn.",
                        Data = null
                    });
                }
                var result = await _authService.RefreshTokenAsync(refreshToken);
                if (result == null)
                {
                    // Nếu failed (hết hạn, bị hack, v.v.), xóa luôn cái cookie cũ ở trình duyệt
                    Response.Cookies.Delete("refreshToken");
                    return Unauthorized(new ApiResponse<object>
                    {
                        Status = 401,
                        Message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.",
                        Data = null
                    });
                }

                SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiredAtUtc);
                var clientResponse = _mapper.Map<ClientAuthResponseDto>(result);
                return Ok(new ApiResponse<ClientAuthResponseDto>
                {
                    Status = 200,
                    Message = "Làm mới token thành công.",
                    Data = clientResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController] : Lỗi hệ thống khi refresh token.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "Lỗi máy chủ.",
                    Data = null
                });
            }
        }
        [Authorize]
        [HttpPost("revoke-refresh-token")]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] RevokeRefreshTokenRequestDto dto)
        {
            try
            {
                _logger.LogInformation("[AuthController] : Nhận request revoke refresh token.");

                var result = await _authService.RevokeRefreshTokenAsync(dto.RefreshToken);

                if (!result)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Status = 400,
                        Message = "Refresh token không hợp lệ hoặc đã bị thu hồi.",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Status = 200,
                    Message = "Thu hồi refresh token thành công.",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController] : Lỗi hệ thống khi revoke refresh token.");
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = 500,
                    Message = "Lỗi máy chủ.",
                    Data = null
                });
            }
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                // Gọi hàm trong AuthService để Update RevokedAtUtc = DateTime.UtcNow
                await _authService.RevokeRefreshTokenAsync(refreshToken);
            }

            // Xóa cookie trên trình duyệt
            Response.Cookies.Delete("refreshToken");

            return Ok(new ApiResponse<object>
            {
                Status = 200,
                Message = "Đăng xuất thành công.",
                Data = null
            });
        }

        private void SetRefreshTokenCookie(string token, DateTime expiresAt) 
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Bắt buộc: JS ở Client không thể đọc được
                Secure = true,  // Bắt buộc: Chỉ gửi qua kết nối HTTPS (Cần thiết trên Production)
                SameSite = SameSiteMode.Strict,// Ngăn chặn tấn công CSRF
                Expires = expiresAt // Cấu hình thời gian hết hạn khớp với hạn của Refresh Token
            };

            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

    }
}
