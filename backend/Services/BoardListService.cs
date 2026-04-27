using TaskHub.backend.Models;
using TaskHub.backend.Repositories.Interfaces;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Services;

public class BoardListService : IBoardListService
{
    private readonly IBoardListRepository _listRepository;
    private readonly IBoardRepository _boardRepository;

    public BoardListService(IBoardListRepository listRepository, IBoardRepository boardRepository)
    {
        _listRepository = listRepository;
        _boardRepository = boardRepository;
    }

    private async Task<bool> IsUserBoardOwner(Guid boardId, Guid userId, CancellationToken ct)
    {
        var board = await _boardRepository.GetBoardByIdAsync(boardId, ct);
        return board != null && board.OwnerId == userId;
    }

    public async Task<IEnumerable<BoardList>> GetListsAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        if (!await IsUserBoardOwner(boardId, userId, ct)) return new List<BoardList>();
        return await _listRepository.GetListsByBoardIdAsync(boardId, ct);
    }

    public async Task<BoardList?> CreateListAsync(BoardList list, Guid userId, CancellationToken ct = default)
    {
        if (!await IsUserBoardOwner(list.BoardId, userId, ct)) return null;
        return await _listRepository.CreateListAsync(list, ct);
    }

    public async Task<bool> UpdateListAsync(Guid listId, BoardList updatedList, Guid userId, CancellationToken ct = default)
    {
        var existingList = await _listRepository.GetListByIdAsync(listId, ct);
        if (existingList == null) return false;
        if (!await IsUserBoardOwner(existingList.BoardId, userId, ct)) return false;

        existingList.Name = updatedList.Name;
        existingList.Position = updatedList.Position;
        existingList.Color = updatedList.Color;
        existingList.UpdatedAt = DateTime.UtcNow;

        await _listRepository.UpdateListAsync(existingList, ct);
        return true;
    }

    public async Task<bool> DeleteListAsync(Guid listId, Guid userId, CancellationToken ct = default)
    {
        var existingList = await _listRepository.GetListByIdAsync(listId, ct);
        if (existingList == null) return false;
        if (!await IsUserBoardOwner(existingList.BoardId, userId, ct)) return false;

        await _listRepository.DeleteListAsync(listId, ct);
        return true;
    }
}
