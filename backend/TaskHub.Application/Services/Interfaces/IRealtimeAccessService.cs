namespace TaskHub.Application.Services.Interfaces;

public interface IRealtimeAccessService
{
    Task AuthorizeBoardAsync(Guid userId, Guid boardId, CancellationToken ct = default);
    Task AuthorizeTaskAsync(Guid userId, Guid taskId, CancellationToken ct = default);
    Task AuthorizeProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task AuthorizeTeamAsync(Guid userId, Guid teamId, CancellationToken ct = default);
    Task<(string FullName, string AvatarUrl)> GetUserDisplayAsync(Guid userId, CancellationToken ct = default);
}
