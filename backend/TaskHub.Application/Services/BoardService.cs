using TaskHub.Application.DTOs;
using TaskHub.Domain.Exceptions;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.Application.Services;

public class BoardService : IBoardService
{
    private readonly IBoardReadRepository _reads;
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IPermissionService _permissionService;

    public BoardService(
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IPermissionService permissionService, IBoardReadRepository reads)
    {
        _reads = reads;
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _permissionService = permissionService;
    }

    public async Task<ProjectKanbanResponseDto> GetProjectKanbanAsync(Guid projectId, Guid userId, KanbanQueryDto query, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, ct) ?? throw new NotFoundException("Project", projectId);
        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.View, ct);
        var result = await _reads.GetKanbanAsync(projectId, query, ct);
        if (result.Board is null) return result;
        var board = new Board { Id=result.Board.Id, OwnerId=result.Board.OwnerId, ProjectId=projectId, Project=project };
        await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.View, ct);
        try { await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.Update, ct); result.CanManageColumns=true; } catch (ForbiddenException) { }
        try { await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.CreateTask, ct); result.CanCreateTasks=true; } catch (ForbiddenException) { }
        return result;
    }

    public async Task<IEnumerable<BoardSummaryDto>> GetUserBoardsAsync(Guid userId, CancellationToken ct = default)
    {
        var boards = await _boardRepository.GetBoardsByUserIdAsync(userId, ct);
        var visible = new List<BoardSummaryDto>();
        foreach (var board in boards)
        {
            try { await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.View, ct); }
            catch (ForbiddenException) { continue; }
            visible.Add(BoardMapping.Summary(board));
        }
        return visible;
    }

    public async Task<IEnumerable<BoardSummaryDto>> GetProjectBoardsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, ct)
            ?? throw new NotFoundException("Project", projectId);

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.View, ct);

        var boards = await _boardRepository.GetBoardsByProjectIdAsync(projectId, ct);
        return boards.Select(BoardMapping.Summary);
    }

    public async Task<BoardResponseDto> GetBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        var board = await _boardRepository.GetBoardByIdAsync(boardId, ct);
        if (board == null) throw new NotFoundException("Board", boardId);

        await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.View, ct);

        board.Lists = board.Lists.OrderBy(l => l.Position).ToList();
        foreach (var list in board.Lists)
        {
            list.Tasks = list.Tasks.OrderBy(t => t.Position).ToList();
        }

        return BoardMapping.Detail(board);
    }

    public async Task<BoardResponseDto> CreateBoardAsync(CreateBoardDto dto, Guid userId, CancellationToken ct = default)
    {
        var board = new Board
        {
            Name = dto.Name,
            Color = dto.Color,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };
        return BoardMapping.Detail(await _boardRepository.CreateBoardAsync(board, ct));
    }

    public async Task<bool> UpdateBoardAsync(Guid boardId, UpdateBoardDto dto, Guid userId, CancellationToken ct = default)
    {
        var existingBoard = await _boardRepository.GetBoardByIdAsync(boardId, ct);
        if (existingBoard == null) throw new NotFoundException("Board", boardId);

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
        if (existingBoard == null) throw new NotFoundException("Board", boardId);

        await _permissionService.AuthorizeBoardActionAsync(userId, existingBoard, BoardAction.Delete, ct);

        await _boardRepository.DeleteBoardAsync(boardId, ct);
        return true;
    }
}
