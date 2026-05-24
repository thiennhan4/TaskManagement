using TaskHub.backend.DTOs;
using TaskHub.backend.Models;

namespace TaskHub.backend.Services.Interfaces;

public interface IBoardListService
{
    Task<IEnumerable<BoardList>> GetListsAsync(Guid boardId, Guid userId, CancellationToken ct = default);
    Task<BoardList?> CreateListAsync(CreateBoardListDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> UpdateListAsync(Guid listId, UpdateBoardListDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteListAsync(Guid listId, Guid userId, CancellationToken ct = default);
}
