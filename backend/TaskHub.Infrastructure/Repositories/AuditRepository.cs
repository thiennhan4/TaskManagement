using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
namespace TaskHub.Infrastructure.Repositories;
public sealed class AuditRepository(AppDbContext db) : IAuditRepository
{
    public async Task AppendAsync(AuditLog log, CancellationToken ct = default)
    {
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }
}
