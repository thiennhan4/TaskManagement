using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

[ApiController, Authorize, Route("api/v1")]
public class BoundedReadController(ITaskItemService tasks, ICollaborationReadService reads) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("tasks"), HttpGet("tasks/my-tasks")]
    public async Task<IActionResult> Tasks([FromQuery] TaskFilterDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<TaskResponseDto>>.Ok(await tasks.GetPageAsync(query,UserId,ct)));
    [HttpGet("tasks/summary")]
    public async Task<IActionResult> Summary(CancellationToken ct) => Ok(ApiResponse<TaskSummaryDto>.Ok(await tasks.GetSummaryAsync(UserId,ct)));
    [HttpGet("tasks/{id:guid}")]
    public async Task<IActionResult> TaskHeader(Guid id, CancellationToken ct) => Ok(ApiResponse<TaskDetailResponseDto>.Ok(await tasks.GetTaskByIdAsync(id,UserId,ct)));
    [HttpGet("tasks/calendar")]
    public async Task<IActionResult> Calendar([FromQuery] CalendarFilterDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<TaskCalendarDto>>.Ok(await tasks.GetCalendarPageAsync(query,UserId,ct)));
    [HttpGet("projects")]
    public async Task<IActionResult> Projects([FromQuery] ProjectQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<ProjectResponseDto>>.Ok(await reads.ProjectsAsync(UserId,query,ct)));
    [HttpGet("projects/{id:guid}")]
    public async Task<IActionResult> Project(Guid id, CancellationToken ct) => Ok(ApiResponse<ProjectResponseDto>.Ok(await reads.ProjectAsync(UserId,id,ct)));
    [HttpGet("teams")]
    public async Task<IActionResult> Teams([FromQuery] PageQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<TeamResponseDto>>.Ok(await reads.TeamsAsync(UserId,query,ct)));
    [HttpGet("teams/{id}/members")]
    public async Task<IActionResult> TeamMembers(Guid id, [FromQuery] PageQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<TeamMemberResponseDto>>.Ok(await reads.TeamMembersAsync(UserId,id,query,ct)));
    [HttpGet("teams/{id:guid}")]
    public async Task<IActionResult> Team(Guid id, CancellationToken ct) =>
        Ok(ApiResponse<TeamResponseDto>.Ok(await reads.TeamAsync(UserId,id,ct)));
    [HttpGet("tasks/{id}/attachments")]
    public async Task<IActionResult> Attachments(Guid id, [FromQuery] PageQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<AttachmentResponseDto>>.Ok(await reads.AttachmentsAsync(UserId,id,query,ct)));
    [HttpGet("tasks/{id}/activity-logs")]
    public async Task<IActionResult> Activity(Guid id, [FromQuery] PageQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<PagedResult<ActivityLogResponseDto>>.Ok(await reads.TaskActivityAsync(UserId,id,query,ct)));
}
