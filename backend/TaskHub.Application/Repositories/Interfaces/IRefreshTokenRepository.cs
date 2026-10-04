using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task<bool> TryRotateAsync(string original, RefreshToken replacement, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetValidAsync(string token, CancellationToken ct = default);
    Task RevokeAsync(string token, CancellationToken ct = default);
    Task RevokeAllAsync(Guid userId, CancellationToken ct = default);
}
