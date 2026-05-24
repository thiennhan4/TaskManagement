using System.Text.Json;
using TaskHub.backend.Data;
using TaskHub.backend.Models;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Services;

/// <summary>
/// Centralized audit logging — replaces duplicated LogAudit() in controllers.
/// </summary>
public class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
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
