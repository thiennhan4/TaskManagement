using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.backend.DTOs;
using TaskHub.backend.Models;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.controller;

[Route("api/[controller]")]
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
    public async Task<IActionResult> GetBoards(CancellationToken ct)
    {
        var boards = await _boardService.GetUserBoardsAsync(GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(boards));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBoard(Guid id, CancellationToken ct)
    {
        var board = await _boardService.GetBoardAsync(id, GetCurrentUserId(), ct);
        if (board == null)
            return NotFound(ApiResponse<object>.Fail("Board not found or access denied."));
        return Ok(ApiResponse<object>.Ok(board));
    }

    [HttpPost]
    public async Task<IActionResult> CreateBoard([FromBody] Board board, CancellationToken ct)
    {
        var createdBoard = await _boardService.CreateBoardAsync(board, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetBoard), new { id = createdBoard.Id },
            ApiResponse<object>.Ok(createdBoard, "Board created."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBoard(Guid id, [FromBody] Board board, CancellationToken ct)
    {
        var result = await _boardService.UpdateBoardAsync(id, board, GetCurrentUserId(), ct);
        if (!result)
            return NotFound(ApiResponse<object>.Fail("Cannot update. Board not found or access denied."));
        return Ok(ApiResponse<object>.Ok(null!, "Board updated."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBoard(Guid id, CancellationToken ct)
    {
        var result = await _boardService.DeleteBoardAsync(id, GetCurrentUserId(), ct);
        if (!result)
            return BadRequest(ApiResponse<object>.Fail("Cannot delete. Board not found or access denied."));
        return Ok(ApiResponse<object>.Ok(null!, "Board deleted."));
    }
}
