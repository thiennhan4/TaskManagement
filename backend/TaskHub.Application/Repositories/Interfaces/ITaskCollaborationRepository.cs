using TaskHub.Domain.Entities;
namespace TaskHub.Application.Repositories.Interfaces;
public interface ITaskCollaborationRepository
{
    Task<int> GetMaxPositionAsync(Guid listId, CancellationToken ct);
    Task<BoardList> EnsurePersonalListAsync(Guid userId, CancellationToken ct);
    Task AddAttachmentAsync(TaskAttachment attachment, CancellationToken ct);
    Task<List<TaskAttachment>> GetAttachmentsAsync(Guid taskId, CancellationToken ct);
    Task<TaskAttachment?> GetAttachmentAsync(Guid id, CancellationToken ct);
    Task DeleteAttachmentAsync(TaskAttachment attachment, CancellationToken ct);
    Task<List<TaskActivityLog>> GetActivityAsync(Guid taskId, CancellationToken ct);
    Task AddInvitationAsync(TaskInvitation invitation, CancellationToken ct);
    Task<TaskInvitation?> GetInvitationAsync(string token, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
