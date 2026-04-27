using TaskHub.backend.Models;

namespace TaskHub.backend.Services.Interfaces;

public interface IBoardService
{
    Task<IEnumerable<Board>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default);
    Task<Board?> GetBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default);
    Task<Board> CreateBoardAsync(Board board, Guid userId, CancellationToken ct = default);
    Task<bool> UpdateBoardAsync(Guid boardId, Board updatedBoard, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default);
}
