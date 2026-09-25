using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;

namespace TaskHub.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AppUser user, CancellationToken ct = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);

    public async Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Users.FindAsync(new object[] { userId }, ct);
    }

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);
    }

    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId && u.Role == UserRole.Admin, ct);
    }
}
