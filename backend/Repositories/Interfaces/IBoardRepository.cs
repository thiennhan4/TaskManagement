using TaskHub.backend.Models;

namespace TaskHub.backend.Repositories.Interfaces;

public interface IBoardRepository
{
    Task<IEnumerable<Board>> GetBoardsByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Board>> GetBoardsByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task<Board?> GetBoardByIdAsync(Guid id, CancellationToken ct = default);
    Task<Board> CreateBoardAsync(Board board, CancellationToken ct = default);
    Task UpdateBoardAsync(Board board, CancellationToken ct = default);
    Task DeleteBoardAsync(Guid id, CancellationToken ct = default);
}
