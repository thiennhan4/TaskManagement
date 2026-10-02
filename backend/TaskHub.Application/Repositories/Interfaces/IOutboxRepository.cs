using TaskHub.Domain.Entities;
namespace TaskHub.Application.Repositories.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
    Task CompleteAsync(Guid id, CancellationToken ct = default);
    Task<List<OutboxMessage>> GetPendingFileCleanupAsync(CancellationToken ct = default);
}
