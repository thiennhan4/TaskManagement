using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services.Interfaces;

public interface IBoardService
{
    Task<ProjectKanbanResponseDto> GetProjectKanbanAsync(Guid projectId, Guid userId, KanbanQueryDto query, CancellationToken ct = default);
    Task<IEnumerable<BoardSummaryDto>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<BoardSummaryDto>> GetProjectBoardsAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task<BoardResponseDto> GetBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default, KanbanQueryDto? query = null);
    Task<BoardResponseDto> CreateBoardAsync(CreateBoardDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> UpdateBoardAsync(Guid boardId, UpdateBoardDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default);
}





