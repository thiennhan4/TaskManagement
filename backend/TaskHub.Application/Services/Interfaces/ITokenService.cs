using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services.Interfaces;

public interface ITokenService
{
    string GenerateJwtToken(AppUser user);
    Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, CancellationToken ct = default);
    Task<RefreshToken?> ValidateRefreshTokenAsync(string token, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default);
}





