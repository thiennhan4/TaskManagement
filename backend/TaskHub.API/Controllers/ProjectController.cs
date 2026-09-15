using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;

namespace TaskHub.API.Controllers;

[Route("api/projects")]
[ApiController]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IBoardService _boardService;

    public ProjectController(IProjectService projectService, IBoardService boardService)
    {
        _projectService = projectService;
        _boardService = boardService;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetUserProjects()
    {
        var projects = await _projectService.GetUserProjectsAsync(GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(projects));
    }

    [HttpGet("workspace/{workspaceId}")]
    public async Task<IActionResult> GetWorkspaceProjects(Guid workspaceId, [FromQuery] bool includeArchived = false)
    {
        var projects = await _projectService.GetWorkspaceProjectsAsync(workspaceId, GetCurrentUserId(), includeArchived);
        return Ok(ApiResponse<object>.Ok(projects));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var project = await _projectService.GetProjectByIdAsync(id, GetCurrentUserId());
        if (project == null) return NotFound(ApiResponse<object>.Fail("Project not found or access denied."));
        return Ok(ApiResponse<object>.Ok(project));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
    {
        var createdProject = await _projectService.CreateProjectAsync(dto, GetCurrentUserId());
        return CreatedAtAction(nameof(GetProject), new { id = createdProject.Id },
            ApiResponse<object>.Ok(createdProject, "Project created successfully."));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectDto dto)
    {
        var updatedProject = await _projectService.UpdateProjectAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(updatedProject, "Project updated successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        await _projectService.DeleteProjectAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(null!, "Project deleted successfully."));
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveProject(Guid id)
    {
        var project = await _projectService.ArchiveProjectAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(project, "Project archived successfully."));
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreProject(Guid id)
    {
        var project = await _projectService.RestoreProjectAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(project, "Project restored successfully."));
    }

    [HttpGet("{id}/activity")]
    public async Task<IActionResult> GetActivity(Guid id)
    {
        var logs = await _projectService.GetActivityLogsAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(logs));
    }

    // â”€â”€â”€ Members & Invitations â”€â”€â”€

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var members = await _projectService.GetMembersAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(members));
    }

    [HttpPost("{id}/members/invite")]
    public async Task<IActionResult> InviteMember(Guid id, [FromBody] InviteMemberDto dto)
    {
        await _projectService.InviteMemberAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(null!, $"Invitation sent to {dto.Email}."));
    }

    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationDto dto)
    {
        await _projectService.AcceptInvitationAsync(dto.Token, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(null!, "Project invitation accepted successfully."));
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        await _projectService.RemoveMemberAsync(id, userId, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(null!, "Member removed successfully."));
    }

    [HttpPatch("{id}/members/{userId}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, Guid userId, [FromBody] UpdateProjectMemberRoleDto dto)
    {
        await _projectService.UpdateMemberRoleAsync(id, userId, dto, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(null!, "Member role updated successfully."));
    }

    // â”€â”€â”€ Boards â”€â”€â”€

    [HttpGet("{id}/boards")]
    public async Task<IActionResult> GetProjectBoards(Guid id, CancellationToken ct)
    {
        var boards = await _boardService.GetProjectBoardsAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(boards));
    }
}



