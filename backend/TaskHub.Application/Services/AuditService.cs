using System.Text.Json;
using TaskHub.Application.Data;
using TaskHub.Domain.Entities;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.Application.Services;

/// <summary>
/// Centralized audit logging â€” replaces duplicated LogAudit() in controllers.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IAppDbContext _context;

    public AuditService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(Guid userId, string action, string entityType, Guid? entityId, object? detail = null, CancellationToken ct = default)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail != null ? JsonSerializer.Serialize(detail) : null,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(ct);
    }
}





