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
    public async Task<IActionResult> TransferOwnership(Guid id, [FromBody] TransferOwnershipDto dto, CancellationToken ct)
    {
        await _projectService.TransferOwnershipAsync(id, GetCurrentUserId(), dto.TargetUserId, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Ownership transferred."));
    }

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
        if (project == null) throw new NotFoundException("Project", id);
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

    [HttpPost("/api/v1/projects/{id}/convert-to-team")]
    public async Task<IActionResult> ConvertToTeam(Guid id, [FromBody] ConvertProjectToTeamDto dto)
    {
        var converted = await _projectService.ConvertToTeamAsync(id, dto.TeamId, GetCurrentUserId());
        return Ok(ApiResponse<ProjectResponseDto>.Ok(converted, "Project converted to a team project."));
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



    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationDto dto)
    {
        await _projectService.AcceptInvitationAsync(dto.Token, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(null!, "Project invitation accepted successfully."));
    }



    // â”€â”€â”€ Boards â”€â”€â”€

    [HttpGet("/api/v1/projects/{id}/kanban")]
    public async Task<IActionResult> GetKanban(Guid id, [FromQuery] KanbanQueryDto query, CancellationToken ct) =>
        Ok(ApiResponse<ProjectKanbanResponseDto>.Ok(await _boardService.GetProjectKanbanAsync(id, GetCurrentUserId(), query, ct)));

    [HttpGet("{id}/boards")]
    public async Task<IActionResult> GetProjectBoards(Guid id, [FromQuery] PageQueryDto query, [FromServices] ICollaborationReadService reads, CancellationToken ct)
    {
        var boards = await reads.BoardsAsync(GetCurrentUserId(), id, query, ct);
        return Ok(ApiResponse<object>.Ok(boards.Items));
    }
}



