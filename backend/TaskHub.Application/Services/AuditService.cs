using System.Text.Json;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.Application.Services;

/// <summary>
/// Centralized audit logging â€” replaces duplicated LogAudit() in controllers.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IAuditRepository _repository;

    public AuditService(IAuditRepository context)
    {
        _repository = context;
    }

    public async Task LogAsync(Guid userId, string action, string entityType, Guid? entityId, object? detail = null, CancellationToken ct = default)
    {
        await _repository.AppendAsync(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail != null ? JsonSerializer.Serialize(detail) : null,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}





