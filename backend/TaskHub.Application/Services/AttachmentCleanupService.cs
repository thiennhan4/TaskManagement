using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
namespace TaskHub.Application.Services;

public sealed class AttachmentCleanupService(IOutboxRepository outbox, IAttachmentStorage storage, ILogger<AttachmentCleanupService> logger)
{
    public async Task RunBatchAsync(CancellationToken ct)
    {
        foreach (var message in await outbox.GetPendingFileCleanupAsync(ct))
        {
            try
            {
                using var payload = JsonDocument.Parse(message.Payload);
                await storage.DeleteAsync(payload.RootElement.GetProperty("Key").GetString()!, ct);
                await outbox.CompleteAsync(message.Id, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception)
            {
                // A bad legacy key must not block other files in this batch. Retain its intent.
                logger.LogWarning("Attachment cleanup {OutboxId} remains pending", message.Id);
            }
        }
    }
}
