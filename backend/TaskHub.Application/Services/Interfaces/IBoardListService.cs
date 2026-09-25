using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services.Interfaces;

public interface IBoardListService
{
    Task<IEnumerable<BoardListResponseDto>> GetListsAsync(Guid boardId, Guid userId, CancellationToken ct = default);
    Task<BoardListResponseDto> CreateListAsync(CreateBoardListDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> UpdateListAsync(Guid listId, UpdateBoardListDto dto, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteListAsync(Guid listId, Guid userId, CancellationToken ct = default);
}





