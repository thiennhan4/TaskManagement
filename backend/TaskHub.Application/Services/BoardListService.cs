using TaskHub.Domain.Exceptions;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using TaskHub.Application.Hubs;

namespace TaskHub.Application.Services;

public class BoardListService : IBoardListService
{
    private readonly IBoardListRepository _listRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IPermissionService _permissions;
    private readonly IProtectedHubContext _hubContext;

    public BoardListService(
        IBoardListRepository listRepository,
        IBoardRepository boardRepository,
        IProtectedHubContext hubContext, IPermissionService permissions)
    {
        _permissions = permissions;
        _listRepository = listRepository;
        _boardRepository = boardRepository;
        _hubContext = hubContext;
    }

    private async Task AuthorizeBoard(Guid boardId, Guid userId, BoardAction action, CancellationToken ct)
    {
        var board = await _boardRepository.GetBoardByIdAsync(boardId, ct)
            ?? throw new NotFoundException("Board", boardId);
        await _permissions.AuthorizeBoardActionAsync(userId, board, action, ct);
    }

    public async Task<IEnumerable<BoardListResponseDto>> GetListsAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        await AuthorizeBoard(boardId, userId, BoardAction.View, ct);
        return (await _listRepository.GetListsByBoardIdAsync(boardId, ct)).Select(BoardMapping.Column);
    }

    public async Task<BoardListResponseDto> CreateListAsync(CreateBoardListDto dto, Guid userId, CancellationToken ct = default)
    {
        await AuthorizeBoard(dto.BoardId, userId, BoardAction.Update, ct);

        var list = new BoardList
        {
            Name = dto.Name,
            BoardId = dto.BoardId,
            Position = dto.Position,
            Color = dto.Color,
            CreatedAt = DateTime.UtcNow
        };

        var createdList = await _listRepository.CreateListAsync(list, ct);
        if (createdList != null)
        {
            await _hubContext.Clients.Group($"board_{dto.BoardId}").SendAsync("BoardListCreated", new
            {
                Id = createdList.Id,
                Name = createdList.Name,
                Position = createdList.Position,
                BoardId = createdList.BoardId,
                Color = createdList.Color,
                CreatedAt = createdList.CreatedAt,
                UpdatedAt = createdList.UpdatedAt
            });
        }
        return BoardMapping.Column(createdList!);
    }

    public async Task<bool> UpdateListAsync(Guid listId, UpdateBoardListDto dto, Guid userId, CancellationToken ct = default)
    {
        var existingList = await _listRepository.GetListByIdAsync(listId, ct);
        if (existingList == null) throw new NotFoundException("BoardList", listId);
        await AuthorizeBoard(existingList.BoardId, userId, BoardAction.Update, ct);

        existingList.Name = dto.Name;
        existingList.Position = dto.Position;
        existingList.Color = dto.Color;
        existingList.UpdatedAt = DateTime.UtcNow;

        await _listRepository.UpdateListAsync(existingList, ct);

        await _hubContext.Clients.Group($"board_{existingList.BoardId}").SendAsync("BoardListUpdated", new
        {
            Id = existingList.Id,
            Name = existingList.Name,
            Position = existingList.Position,
            BoardId = existingList.BoardId,
            Color = existingList.Color,
            UpdatedAt = existingList.UpdatedAt
        });

        return true;
    }

    public async Task<bool> DeleteListAsync(Guid listId, Guid userId, CancellationToken ct = default)
    {
        var existingList = await _listRepository.GetListByIdAsync(listId, ct);
        if (existingList == null) throw new NotFoundException("BoardList", listId);
        await AuthorizeBoard(existingList.BoardId, userId, BoardAction.Update, ct);

        await _listRepository.DeleteListAsync(listId, ct);

        await _hubContext.Clients.Group($"board_{existingList.BoardId}").SendAsync("BoardListDeleted", listId);

        return true;
    }
}





