using AutoMapper;
using BCrypt.Net;
using LaptopStore.Repositories.Entities;
using LaptopStore.Repositories.Interfaces;
using LaptopStore.Services.DTOs.Auth;
using LaptopStore.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using LaptopStore.Services.Configurations;
namespace LaptopStore.Services.Implements
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AuthService> logger, ITokenService tokenService, IOptions<JwtSettings> JwtOptions)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _tokenService = tokenService;
            _jwtSettings = JwtOptions.Value;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            _logger.LogInformation("[AuthService] : Bắt đầu xử lý đăng nhập cho email {Email}.", dto.Email);
            User? user = await _unitOfWork.Users.GetAsync(u => u.Email.ToLower() == dto.Email.ToLower(), includeProperties: "Role", tracked: false);
            if (user == null)
            {
                _logger.LogWarning("[AuthService] : Đăng nhập thất bại vì không tìm thấy email {Email}.", dto.Email);
                return null;
            }
            // [AuthService] : So sánh password người dùng nhập với PasswordHash lưu trong DB.
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                _logger.LogWarning("[AuthService] : Đăng nhập thất bại vì sai mật khẩu cho email {Email}.", dto.Email);
                return null;
            }
            _logger.LogInformation("[AuthService] : Đăng nhập thành công cho user {UserId}.", user.Id);
            return await BuildAuthResponseWithRefreshTokenAsync(user);
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            _logger.LogInformation("[AuthService] : Bắt đầu xử lý refresh token từ Cookie.");
            // [AuthService] : Tìm token trong DB
            var tokenEntity = await _unitOfWork.RefreshTokens.GetAsync(
                            rt => rt.Token == refreshToken,
                            includeProperties: "User,User.Role",
                            tracked: true); // Tracked = true để EF Core tự tracking update

            if (tokenEntity == null)
            {
                _logger.LogWarning("[AuthService] : Refresh token không tồn tại trong hệ thống.");
                return null;
            }
            //----------------------------------------------------
            //PHÁT HIỆN TẤN CÔNG (COMPROMISED TOKEN DETECTION)
            //----------------------------------------------------
            if (tokenEntity.RevokedAtUtc != null)
            {
                // Nếu token đã bị revoke mà vẫn được dùng lại -> Có kẻ đã ăn cắp token cũ!
                _logger.LogWarning("[AuthService] : PHÁT HIỆN BẤT THƯỜNG! Token {Token} đã bị revoke nhưng vẫn được sử dụng. Có thể tài khoản {UserId} đang bị tấn công.", tokenEntity.Token, tokenEntity.UserId);

                // HÀNH ĐỘNG: Thu hồi ngay lập tức token hiện tại đang active của user này (hoặc toàn bộ token)
                // Để hacker đang cầm token mới cũng bị văng ra ngoài.
                await RevokeAllTokensForUserAsync(tokenEntity.UserId);
                return null;
            }

            // Kiểm tra hết hạn bình thường
            if (tokenEntity.ExpiresAtUtc <= DateTime.UtcNow)
            {
                _logger.LogInformation("[AuthService] : Refresh token đã hết hạn tự nhiên.");
                return null;
            }

            // ----------------------------------------------------------------------
            // THỰC HIỆN ROTATION (XOAY VÒNG)
            // ----------------------------------------------------------------------
            // 1. Đánh dấu token cũ là đã sử dụng (Revoked)
            tokenEntity.RevokedAtUtc = DateTime.UtcNow;

            // 2. Tạo Token mới
            string newRefreshToken = _tokenService.GenerateRefreshToken();
            DateTime newRefreshTokenExpires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays);

            // 3. Móc nối lịch sử (ReplacedByToken)
            tokenEntity.ReplacedByToken = newRefreshToken;

            // 4. Lưu Token mới vào DB
            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = tokenEntity.UserId,
                Token = newRefreshToken,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = newRefreshTokenExpires
            };
            await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity);

            // 5. Cấp lại Access Token mới
            var accessTokenResult = _tokenService.GenerateAccessToken(tokenEntity.User);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[AuthService] : Refresh token thành công. Đã xoay vòng token cho user {UserId}.", tokenEntity.UserId);
            return new AuthResponseDto
            {
                AccessToken = accessTokenResult.AccessToken,
                ExpiredAtUtc = accessTokenResult.AccessTokenExpiredAtUtc,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiredAtUtc = newRefreshTokenExpires,
                UserId = tokenEntity.User.Id,
                FullName = tokenEntity.User.FullName,
                Email = tokenEntity.User.Email,
                RoleName = tokenEntity.User.Role.Name
            };
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto)
        {
            _logger.LogInformation("[AuthService] : Bắt đầu xử lý đăng ký cho email {Email}.", dto.Email);
            // [AuthService] : Kiểm tra email đã tồn tại chưa để tránh tạo trùng tài khoản.
            User? existingUser = await _unitOfWork.Users.GetAsync(u => u.Email.ToLower() == dto.Email.ToLower(), includeProperties: "Role", tracked: false);
            if (existingUser != null)
            {
                _logger.LogWarning("[AuthService] : Đăng ký thất bại vì email {Email} đã tồn tại.", dto.Email);
                return null;
            }
            // [AuthService] : Tìm role Customer để gán mặc định cho user mới đăng ký.
            Role? customerRole = await _unitOfWork.Roles.GetAsync(r => r.Name == "Customer", tracked: false);
            if (customerRole == null)
            {
                _logger.LogError("[AuthService] : Không tìm thấy role Customer trong database.");
                throw new Exception("Role Customer chưa được seed trong database.");
            }
            // [AuthService] : Hash password trước khi lưu để nếu DB lộ thì password gốc vẫn không bị lộ theo.
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var newUser = _mapper.Map<User>(dto);
            newUser.PasswordHash = passwordHash;
            newUser.RoleId = customerRole.Id;

            await _unitOfWork.Users.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            // [AuthService] : Đọc lại user kèm role để tạo token đầy đủ claim.
            User? createdUser = await _unitOfWork.Users.GetAsync(u => u.Id == newUser.Id, includeProperties: "Role", tracked: false);

            _logger.LogInformation("[AuthService] : Đăng ký thành công cho user {UserId}.", newUser.Id);

            return await BuildAuthResponseWithRefreshTokenAsync(createdUser!);
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            _logger.LogInformation("[AuthService] : Bắt đầu revoke refresh token.");

            var storedToken = await _unitOfWork.RefreshTokens.GetAsync(
                x => x.Token == refreshToken,
                tracked: true);

            if (storedToken == null)
            {
                _logger.LogWarning("[AuthService] : Revoke thất bại vì không tìm thấy refresh token trong DB.");
                return false;
            }

            if (storedToken.RevokedAtUtc != null)
            {
                _logger.LogWarning("[AuthService] : Revoke thất bại vì refresh token đã bị revoke trước đó.");
                return false;
            }

            storedToken.RevokedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[AuthService] : Revoke refresh token thành công.");
            return true;
        }

        private async Task<AuthResponseDto> BuildAuthResponseWithRefreshTokenAsync(User user)
        {
            // [AuthService] : Gọi TokenService để sinh access token thay vì tự xử lý JWT trong AuthService.
            var accessTokenResult = _tokenService.GenerateAccessToken(user);
            // [AuthService] : Gọi TokenService để sinh refresh token ngẫu nhiên.
            string refreshToken = _tokenService.GenerateRefreshToken();
            DateTime refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays);
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = refreshTokenExpiresAtUtc,
            };
            await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = accessTokenResult.AccessToken,
                ExpiredAtUtc = accessTokenResult.AccessTokenExpiredAtUtc,
                RefreshToken = refreshToken,
                RefreshTokenExpiredAtUtc = refreshTokenExpiresAtUtc,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = user.Role.Name
            };
        }
        // Hàm phụ trợ để xử lý khi phát hiện hack
        private async Task RevokeAllTokensForUserAsync(Guid userId)
        {
            var activeTokens = await _unitOfWork.RefreshTokens.GetAllAsync(rt => rt.UserId == userId && rt.RevokedAtUtc == null, tracked: true);
            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = DateTime.UtcNow;
            }
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("[AuthService] : Đã thu hồi toàn bộ Refresh Token active của user {UserId} vì lý do bảo mật.", userId);
        }

    }
}
