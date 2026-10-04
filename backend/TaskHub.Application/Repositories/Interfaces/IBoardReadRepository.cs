using TaskHub.Application.DTOs;

namespace TaskHub.Application.Repositories.Interfaces;

public interface IBoardReadRepository
{
    Task<PagedResult<KanbanColumnDto>> GetColumnsAsync(Guid boardId, KanbanQueryDto query, CancellationToken ct);
    Task<ProjectKanbanResponseDto> GetKanbanAsync(Guid projectId, KanbanQueryDto query, CancellationToken ct);
}
