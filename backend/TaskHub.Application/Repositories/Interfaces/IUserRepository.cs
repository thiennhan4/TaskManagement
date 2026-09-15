using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsAdminAsync(Guid userId, CancellationToken ct = default);
}
