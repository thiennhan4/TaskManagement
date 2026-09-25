using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;

namespace TaskHub.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync(ct);
    }

    public Task<RefreshToken?> GetValidAsync(string token, CancellationToken ct = default) =>
        context.RefreshTokens.Include(rt => rt.User).FirstOrDefaultAsync(
            rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow, ct);

    public async Task RevokeAsync(string token, CancellationToken ct = default)
    {
        var existing = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token, ct);
        if (existing is null) return;
        existing.IsRevoked = true;
        existing.RevokedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync(ct);
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync(ct);
    }
}
