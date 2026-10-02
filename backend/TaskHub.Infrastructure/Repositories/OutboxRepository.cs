using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
namespace TaskHub.Infrastructure.Repositories;

public sealed class OutboxRepository(AppDbContext db) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default)
    {
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync(ct);
    }
    public async Task CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var message = await db.OutboxMessages.SingleAsync(m => m.Id == id, ct);
        message.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
    public Task<List<OutboxMessage>> GetPendingFileCleanupAsync(CancellationToken ct = default) =>
        db.OutboxMessages.Where(m => m.Kind == "FileCleanup" && m.CompletedAt == null)
            .OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).Take(100).ToListAsync(ct);
}
