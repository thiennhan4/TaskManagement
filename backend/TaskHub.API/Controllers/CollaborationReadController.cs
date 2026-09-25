using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

[ApiController, Authorize, Route("api/v1")]
public class CollaborationReadController(ICollaborationReadService reads) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("boards")]
    public async Task<IActionResult> Boards([FromQuery] PageQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<BoardSummaryDto>>.Ok(await reads.BoardsAsync(UserId, null, query, ct)));
    [HttpGet("projects/{projectId}/boards")]
    public async Task<IActionResult> ProjectBoards(Guid projectId, [FromQuery] PageQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<BoardSummaryDto>>.Ok(await reads.BoardsAsync(UserId, projectId, query, ct)));
    [HttpGet("boardlists/board/{boardId}")]
    public async Task<IActionResult> Columns(Guid boardId, [FromQuery] KanbanQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<KanbanColumnDto>>.Ok(await reads.ColumnsAsync(UserId, boardId, query, ct)));
    [HttpGet("tasks/{taskId}/comments")]
    public async Task<IActionResult> Comments(Guid taskId, [FromQuery] PageQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<CommentResponseDto>>.Ok(await reads.CommentsAsync(UserId, taskId, query, ct)));
    [HttpGet("projects/{projectId}/members")]
    public async Task<IActionResult> Members(Guid projectId, [FromQuery] PageQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<ProjectMemberDto>>.Ok(await reads.MembersAsync(UserId, projectId, query, ct)));
}
