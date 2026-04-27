using TaskHub.backend.Models;
using TaskHub.backend.Repositories.Interfaces;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Services;

public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;

    public BoardService(IBoardRepository boardRepository)
    {
        _boardRepository = boardRepository;
    }

    public async Task<IEnumerable<Board>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _boardRepository.GetBoardsByUserIdAsync(userId, ct);
    }

    public async Task<Board?> GetBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        var board = await _boardRepository.GetBoardByIdAsync(boardId, ct);
        if (board == null || board.OwnerId != userId) return null;

        board.Lists = board.Lists.OrderBy(l => l.Position).ToList();
        foreach (var list in board.Lists)
        {
            list.Tasks = list.Tasks.OrderBy(t => t.Position).ToList();
        }

        return board;
    }

    public async Task<Board> CreateBoardAsync(Board board, Guid userId, CancellationToken ct = default)
    {
        board.OwnerId = userId;
        board.CreatedAt = DateTime.UtcNow;
        return await _boardRepository.CreateBoardAsync(board, ct);
    }

    public async Task<bool> UpdateBoardAsync(Guid boardId, Board updatedBoard, Guid userId, CancellationToken ct = default)
    {
        var existingBoard = await _boardRepository.GetBoardByIdAsync(boardId, ct);
        if (existingBoard == null || existingBoard.OwnerId != userId) return false;

        existingBoard.Name = updatedBoard.Name;
        existingBoard.Color = updatedBoard.Color;
        existingBoard.UpdatedAt = DateTime.UtcNow;

        await _boardRepository.UpdateBoardAsync(existingBoard, ct);
        return true;
    }

    public async Task<bool> DeleteBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        var existingBoard = await _boardRepository.GetBoardByIdAsync(boardId, ct);
        if (existingBoard == null || existingBoard.OwnerId != userId) return false;

        await _boardRepository.DeleteBoardAsync(boardId, ct);
        return true;
    }
}
