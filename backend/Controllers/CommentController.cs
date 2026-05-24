using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.backend.DTOs;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Controllers;

[Route("api/tasks")]
[ApiController]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── GET /api/tasks/{taskId}/comments ───
    [HttpGet("{taskId}/comments")]
    public async Task<IActionResult> GetComments(Guid taskId, CancellationToken ct)
    {
        var comments = await _commentService.GetCommentsByTaskAsync(taskId, GetCurrentUserId(), ct);
        return Ok(ApiResponse<IEnumerable<CommentResponseDto>>.Ok(comments));
    }

    // ─── POST /api/tasks/{taskId}/comments ───
    [HttpPost("{taskId}/comments")]
    public async Task<IActionResult> CreateComment(Guid taskId, [FromBody] CreateCommentDto dto, CancellationToken ct)
    {
        var comment = await _commentService.CreateCommentAsync(taskId, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<CommentResponseDto>.Ok(comment, "Comment added."));
    }

    // ─── DELETE /api/tasks/comments/{commentId} ───
    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken ct)
    {
        await _commentService.DeleteCommentAsync(commentId, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Comment deleted."));
    }
}
