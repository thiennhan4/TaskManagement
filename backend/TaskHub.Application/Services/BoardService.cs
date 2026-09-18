using TaskHub.Application.DTOs;
using TaskHub.Domain.Exceptions;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.Application.Services;

public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IPermissionService _permissionService;

    public BoardService(
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IPermissionService permissionService)
    {
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _permissionService = permissionService;
    }

    public async Task<IEnumerable<Board>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _boardRepository.GetBoardsByUserIdAsync(userId, ct);
    }

    public async Task<IEnumerable<Board>> GetProjectBoardsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, ct)
            ?? throw new NotFoundException("Project", projectId);

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.View, ct);

        var boards = await _boardRepository.GetBoardsByProjectIdAsync(projectId, ct);
        return boards;
    }

    public async Task<Board?> GetBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        var board = await _boardRepository.GetBoardByIdAsync(boardId, ct);
        if (board == null) return null;

        await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.View, ct);

        board.Lists = board.Lists.OrderBy(l => l.Position).ToList();
        foreach (var list in board.Lists)
        {
            list.Tasks = list.Tasks.OrderBy(t => t.Position).ToList();
        }

        return board;
    }

    public async Task<Board> CreateBoardAsync(CreateBoardDto dto, Guid userId, CancellationToken ct = default)
    {
        var board = new Board
        {
            Name = dto.Name,
            Color = dto.Color,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };
        return await _boardRepository.CreateBoardAsync(board, ct);
    }

    public async Task<bool> UpdateBoardAsync(Guid boardId, UpdateBoardDto dto, Guid userId, CancellationToken ct = default)
    {
        var existingBoard = await _boardRepository.GetBoardByIdAsync(boardId, ct);
        if (existingBoard == null) return false;

        await _permissionService.AuthorizeBoardActionAsync(userId, existingBoard, BoardAction.Update, ct);

        existingBoard.Name = dto.Name;
        existingBoard.Color = dto.Color;
        existingBoard.UpdatedAt = DateTime.UtcNow;

        await _boardRepository.UpdateBoardAsync(existingBoard, ct);
        return true;
    }

    public async Task<bool> DeleteBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        var existingBoard = await _boardRepository.GetBoardByIdAsync(boardId, ct);
        if (existingBoard == null) return false;

        await _permissionService.AuthorizeBoardActionAsync(userId, existingBoard, BoardAction.Delete, ct);

        await _boardRepository.DeleteBoardAsync(boardId, ct);
        return true;
    }
}
