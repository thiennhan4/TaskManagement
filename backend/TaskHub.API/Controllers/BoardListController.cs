using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

[Route("api/boardlists")]
[ApiController]
[Authorize]
public class BoardListController : ControllerBase
{
    private readonly IBoardListService _listService;

    public BoardListController(IBoardListService listService)
    {
        _listService = listService;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("board/{boardId}")]
    public async Task<IActionResult> GetListsByBoard(Guid boardId, [FromQuery] KanbanQueryDto query, [FromServices] ICollaborationReadService reads, CancellationToken ct)
    {
        var lists = await reads.ColumnsAsync(GetCurrentUserId(), boardId, query, ct);
        return Ok(ApiResponse<object>.Ok(lists.Items.Select(l => new BoardListResponseDto
        { Id=l.Id, BoardId=l.BoardId, Name=l.Name, Color=l.Color, Position=l.Position,
            CreatedAt=l.CreatedAt, UpdatedAt=l.UpdatedAt, Tasks=l.Tasks.Items })));
    }

    [HttpPost]
    public async Task<IActionResult> CreateList([FromBody] CreateBoardListDto dto, CancellationToken ct)
    {
        var createdList = await _listService.CreateListAsync(dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(createdList, "List created."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateList(Guid id, [FromBody] UpdateBoardListDto dto, CancellationToken ct)
    {
        var success = await _listService.UpdateListAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "List updated."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteList(Guid id, CancellationToken ct)
    {
        var success = await _listService.DeleteListAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "List deleted."));
    }
}



