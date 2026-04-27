using TaskHub.backend.Models;

namespace TaskHub.backend.Services.Interfaces;

public interface IBoardListService
{
    Task<IEnumerable<BoardList>> GetListsAsync(Guid boardId, Guid userId, CancellationToken ct = default);
    Task<BoardList?> CreateListAsync(BoardList list, Guid userId, CancellationToken ct = default);
    Task<bool> UpdateListAsync(Guid listId, BoardList updatedList, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteListAsync(Guid listId, Guid userId, CancellationToken ct = default);
}
