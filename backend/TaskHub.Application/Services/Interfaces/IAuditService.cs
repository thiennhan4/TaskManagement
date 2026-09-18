namespace TaskHub.Application.Services.Interfaces;

/// <summary>
/// Centralized audit logging service â€” replaces duplicated LogAudit helpers.
/// </summary>
public interface IAuditService
{
    Task LogAsync(Guid userId, string action, string entityType, Guid? entityId, object? detail = null, CancellationToken ct = default);
}





