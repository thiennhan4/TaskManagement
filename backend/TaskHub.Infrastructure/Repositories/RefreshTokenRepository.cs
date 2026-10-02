using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;

namespace TaskHub.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public async Task<bool> TryRotateAsync(string original, RefreshToken replacement, CancellationToken ct = default)
    {
        await using var tx = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(ct) : null;
        var now = DateTime.UtcNow;
        var eligible = context.RefreshTokens.Where(t => t.Token == original && t.UserId == replacement.UserId &&
            !t.IsRevoked && t.ExpiresAt > now && t.User.IsActive);
        if (context.Database.IsRelational())
        {
            var changed = await eligible.ExecuteUpdateAsync(set => set.SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, (DateTime?)now), ct);
            if (changed != 1) return false;
        }
        else
        {
            // Functional tests only: InMemory cannot establish SQL concurrency guarantees.
            var token = await eligible.SingleOrDefaultAsync(ct);
            if (token == null) return false;
            token.IsRevoked = true;
            token.RevokedAt = now;
        }
        context.RefreshTokens.Add(replacement);
        await context.SaveChangesAsync(ct);
        if (tx != null) await tx.CommitAsync(ct);
        return true;
    }

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
