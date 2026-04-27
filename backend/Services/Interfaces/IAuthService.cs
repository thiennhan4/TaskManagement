using TaskHub.backend.DTOs;

namespace TaskHub.backend.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<ApiResponse<object>> LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default);
    Task<ApiResponse<UserResponseDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
