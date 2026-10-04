using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Domain.Entities;
namespace TaskHub.Application.Services;

public sealed class DurableDelivery(IOutboxRepository outbox, IMutationRunner mutations, ILogger<DurableDelivery> logger)
{
    public async Task EnqueueAsync(string kind, object payload, Func<Task> deliver, CancellationToken ct = default)
    {
        var message = new OutboxMessage { Kind = kind, Payload = JsonSerializer.Serialize(payload) };
        await outbox.AddAsync(message, ct);
        await mutations.AfterCommitAsync(async () =>
        {
            try
            {
                await deliver();
                await outbox.CompleteAsync(message.Id, CancellationToken.None);
            }
            catch (Exception)
            {
                // Payloads can include invitation tokens; never log payloads or provider exceptions.
                logger.LogWarning("Delivery {OutboxId} of kind {Kind} remains pending", message.Id, kind);
            }
        });
    }
}
