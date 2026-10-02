using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;

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

    [HttpPost("/api/v1/projects/{id}/transfer-ownership")]
    public async Task<IActionResult> TransferOwnership(Guid id, [FromBody] TransferOwnershipDto dto, CancellationToken ct = default)
    {
        await _projectService.TransferOwnershipAsync(id, GetCurrentUserId(), dto.TargetUserId, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Ownership transferred."));
    }

    [HttpGet]
    public async Task<IActionResult> GetUserProjects(CancellationToken ct = default)
    {
        var projects = await _projectService.GetUserProjectsAsync(GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<object>.Ok(projects));
    }

    [HttpGet("workspace/{workspaceId}")]
    public async Task<IActionResult> GetWorkspaceProjects(Guid workspaceId, [FromQuery] bool includeArchived = false, CancellationToken ct = default)
    {
        var projects = await _projectService.GetWorkspaceProjectsAsync(workspaceId, GetCurrentUserId(), includeArchived, ct: ct);
        return Ok(ApiResponse<object>.Ok(projects));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(Guid id, CancellationToken ct = default)
    {
        var project = await _projectService.GetProjectByIdAsync(id, GetCurrentUserId(), ct: ct);
        if (project == null) throw new NotFoundException("Project", id);
        return Ok(ApiResponse<object>.Ok(project));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto, CancellationToken ct = default)
    {
        var createdProject = await _projectService.CreateProjectAsync(dto, GetCurrentUserId(), ct: ct);
        return CreatedAtAction(nameof(GetProject), new { id = createdProject.Id },
            ApiResponse<object>.Ok(createdProject, "Project created successfully."));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectDto dto, CancellationToken ct = default)
    {
        var updatedProject = await _projectService.UpdateProjectAsync(id, dto, GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<object>.Ok(updatedProject, "Project updated successfully."));
    }

    [HttpPost("/api/v1/projects/{id}/convert-to-team")]
    public async Task<IActionResult> ConvertToTeam(Guid id, [FromBody] ConvertProjectToTeamDto dto, CancellationToken ct = default)
    {
        var converted = await _projectService.ConvertToTeamAsync(id, dto.TeamId, GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<ProjectResponseDto>.Ok(converted, "Project converted to a team project."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken ct = default)
    {
        await _projectService.DeleteProjectAsync(id, GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<object>.Ok(null!, "Project deleted successfully."));
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveProject(Guid id, CancellationToken ct = default)
    {
        var project = await _projectService.ArchiveProjectAsync(id, GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<object>.Ok(project, "Project archived successfully."));
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreProject(Guid id, CancellationToken ct = default)
    {
        var project = await _projectService.RestoreProjectAsync(id, GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<object>.Ok(project, "Project restored successfully."));
    }

    [HttpGet("{id}/activity")]
    public async Task<IActionResult> GetActivity(Guid id, CancellationToken ct = default)
    {
        var logs = await _projectService.GetActivityLogsAsync(id, GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<object>.Ok(logs));
    }

    // â”€â”€â”€ Members & Invitations â”€â”€â”€



    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationDto dto, CancellationToken ct = default)
    {
        await _projectService.AcceptInvitationAsync(dto.Token, GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<object>.Ok(null!, "Project invitation accepted successfully."));
    }



    // â”€â”€â”€ Boards â”€â”€â”€

    [HttpGet("/api/v1/projects/{id}/kanban")]
    public async Task<IActionResult> GetKanban(Guid id, [FromQuery] KanbanQueryDto query, CancellationToken ct = default) =>
        Ok(ApiResponse<ProjectKanbanResponseDto>.Ok(await _boardService.GetProjectKanbanAsync(id, GetCurrentUserId(), query, ct)));

    [HttpGet("{id}/boards")]
    public async Task<IActionResult> GetProjectBoards(Guid id, [FromQuery] PageQueryDto query, [FromServices] ICollaborationReadService reads, CancellationToken ct = default)
    {
        var boards = await reads.BoardsAsync(GetCurrentUserId(), id, query, ct);
        return Ok(ApiResponse<object>.Ok(boards.Items));
    }
}



