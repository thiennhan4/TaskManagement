using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

[Route("api/projects/{projectId}/members")]
[ApiController]
[Authorize]
public class ProjectMemberController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectMemberController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMembers(Guid projectId, [FromQuery] PageQueryDto query, [FromServices] ICollaborationReadService reads, CancellationToken ct = default)
    {
        var members = await reads.MembersAsync(GetCurrentUserId(), projectId, query, ct);
        return this.LegacyPage(members);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteMember(Guid projectId, [FromBody] InviteMemberDto dto, CancellationToken ct = default)
    {
        await _projectService.InviteMemberAsync(projectId, dto, GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<object>.Ok(null!, $"Invitation sent to {dto.Email}."));
    }

    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvite(Guid projectId, [FromBody] AcceptInvitationDto dto, CancellationToken ct = default)
    {
        await _projectService.AcceptInvitationAsync(dto.Token, GetCurrentUserId(), projectId, ct: ct);
        return Ok(ApiResponse<object>.Ok(null!, "Invitation accepted. Welcome to the project!"));
    }

    [HttpPatch("{memberUserId}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid projectId, Guid memberUserId, [FromBody] UpdateProjectMemberRoleDto dto, CancellationToken ct = default)
    {
        await _projectService.UpdateMemberRoleAsync(projectId, memberUserId, dto, GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<object>.Ok(null!, "Member role updated."));
    }

    [HttpDelete("{memberUserId}")]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid memberUserId, CancellationToken ct = default)
    {
        await _projectService.RemoveMemberAsync(projectId, memberUserId, GetCurrentUserId(), ct: ct);
        return Ok(ApiResponse<object>.Ok(null!, "Member removed from project."));
    }
}


