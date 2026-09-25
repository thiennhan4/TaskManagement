using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

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

    // â”€â”€â”€ GET /api/tasks/{taskId}/comments â”€â”€â”€
    [HttpGet("{taskId}/comments")]
    public async Task<IActionResult> GetComments(Guid taskId, [FromQuery] PageQueryDto query, [FromServices] ICollaborationReadService reads, CancellationToken ct)
    {
        var comments = await reads.CommentsAsync(GetCurrentUserId(), taskId, query, ct);
        return Ok(ApiResponse<IEnumerable<CommentResponseDto>>.Ok(comments.Items));
    }

    // â”€â”€â”€ POST /api/tasks/{taskId}/comments â”€â”€â”€
    [HttpPost("{taskId}/comments")]
    public async Task<IActionResult> CreateComment(Guid taskId, [FromBody] CreateCommentDto dto, CancellationToken ct)
    {
        var comment = await _commentService.CreateCommentAsync(taskId, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<CommentResponseDto>.Ok(comment, "Comment added."));
    }

    // â”€â”€â”€ DELETE /api/tasks/comments/{commentId} â”€â”€â”€
    [HttpDelete("{taskId}/comments/{commentId}")]
    public async Task<IActionResult> DeleteTaskComment(Guid taskId, Guid commentId, CancellationToken ct)
    {
        await _commentService.DeleteCommentAsync(commentId, GetCurrentUserId(), ct, taskId);
        return Ok(ApiResponse<object>.Ok(null!, "Comment deleted."));
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken ct)
    {
        await _commentService.DeleteCommentAsync(commentId, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Comment deleted."));
    }
}



