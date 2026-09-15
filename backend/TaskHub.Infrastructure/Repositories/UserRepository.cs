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

    public async Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Users.FindAsync(new object[] { userId }, ct);
    }

    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId && u.Role == UserRole.Admin, ct);
    }
}
