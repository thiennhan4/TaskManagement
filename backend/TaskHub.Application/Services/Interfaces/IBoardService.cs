using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services.Interfaces;

public interface IBoardService
{
    Task<IEnumerable<Board>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Board>> GetProjectBoardsAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task<Board?> GetBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default);
    Task<Board> CreateBoardAsync(CreateBoardDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> UpdateBoardAsync(Guid boardId, UpdateBoardDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default);
}





