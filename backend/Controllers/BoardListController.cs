using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.backend.DTOs;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Controllers;

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
    public async Task<IActionResult> GetListsByBoard(Guid boardId, CancellationToken ct)
    {
        var lists = await _listService.GetListsAsync(boardId, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(lists));
    }

    [HttpPost]
    public async Task<IActionResult> CreateList([FromBody] CreateBoardListDto dto, CancellationToken ct)
    {
        var createdList = await _listService.CreateListAsync(dto, GetCurrentUserId(), ct);
        if (createdList == null)
            return BadRequest(ApiResponse<object>.Fail("Cannot create list. Access denied."));
        return Ok(ApiResponse<object>.Ok(createdList, "List created."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateList(Guid id, [FromBody] UpdateBoardListDto dto, CancellationToken ct)
    {
        var success = await _listService.UpdateListAsync(id, dto, GetCurrentUserId(), ct);
        if (!success)
            return NotFound(ApiResponse<object>.Fail("List not found or access denied."));
        return Ok(ApiResponse<object>.Ok(null!, "List updated."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteList(Guid id, CancellationToken ct)
    {
        var success = await _listService.DeleteListAsync(id, GetCurrentUserId(), ct);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail("Cannot delete list."));
        return Ok(ApiResponse<object>.Ok(null!, "List deleted."));
    }
}
