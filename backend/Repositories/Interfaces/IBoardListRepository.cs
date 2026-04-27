using TaskHub.backend.Models;

namespace TaskHub.backend.Repositories.Interfaces;

public interface IBoardListRepository
{
    Task<IEnumerable<BoardList>> GetListsByBoardIdAsync(Guid boardId, CancellationToken ct = default);
    Task<BoardList?> GetListByIdAsync(Guid id, CancellationToken ct = default);
    Task<BoardList> CreateListAsync(BoardList list, CancellationToken ct = default);
    Task UpdateListAsync(BoardList list, CancellationToken ct = default);
    Task DeleteListAsync(Guid id, CancellationToken ct = default);
}
