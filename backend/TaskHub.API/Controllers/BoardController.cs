using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

[Route("api/boards")]
[ApiController]
[Authorize]
public class BoardController : ControllerBase
{
    private readonly IBoardService _boardService;

    public BoardController(IBoardService boardService)
    {
        _boardService = boardService;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetBoards([FromQuery] PageQueryDto query, [FromServices] ICollaborationReadService reads, CancellationToken ct)
    {
        var boards = await reads.BoardsAsync(GetCurrentUserId(), null, query, ct);
        return Ok(ApiResponse<object>.Ok(boards.Items));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBoard(Guid id, CancellationToken ct)
    {
        var board = await _boardService.GetBoardAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(board));
    }

    [HttpPost]
    public async Task<IActionResult> CreateBoard([FromBody] CreateBoardDto dto, CancellationToken ct)
    {
        var createdBoard = await _boardService.CreateBoardAsync(dto, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetBoard), new { id = createdBoard.Id },
            ApiResponse<object>.Ok(createdBoard, "Board created."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBoard(Guid id, [FromBody] UpdateBoardDto dto, CancellationToken ct)
    {
        var result = await _boardService.UpdateBoardAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Board updated."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBoard(Guid id, CancellationToken ct)
    {
        var result = await _boardService.DeleteBoardAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Board deleted."));
    }
}



