using TaskHub.Domain.Entities;
namespace TaskHub.Application.Repositories.Interfaces;
public interface IAuditRepository
{
    Task AppendAsync(AuditLog log, CancellationToken ct = default);
}
