using TaskHub.Application.DTOs;
using TaskHub.Domain.Exceptions;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using FluentValidation;
using TaskHub.Application.Validators;

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
        await new KanbanQueryValidator().ValidateAndThrowAsync(query, ct);
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

    public async Task<BoardResponseDto> GetBoardAsync(Guid boardId, Guid userId, CancellationToken ct = default, KanbanQueryDto? query = null)
    {
        query ??= new KanbanQueryDto();
        await new KanbanQueryValidator().ValidateAndThrowAsync(query, ct);
        var board = await _boardRepository.GetBoardByIdAsync(boardId, ct);
        if (board == null) throw new NotFoundException("Board", boardId);

        await _permissionService.AuthorizeBoardActionAsync(userId, board, BoardAction.View, ct);

        var page=await _reads.GetColumnsAsync(boardId,query,ct);
        var result=BoardMapping.Detail(board);
        result.ListPage=page;
        result.Lists=page.Items.Select(l=>new BoardListResponseDto { Id=l.Id,BoardId=l.BoardId,Name=l.Name,Color=l.Color,Position=l.Position,CreatedAt=l.CreatedAt,UpdatedAt=l.UpdatedAt,Tasks=l.Tasks.Items,TaskPage=l.Tasks }).ToList();
        return result;
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
