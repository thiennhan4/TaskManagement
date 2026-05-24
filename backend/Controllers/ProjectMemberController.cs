using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.backend.DTOs;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Controllers;

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
    public async Task<IActionResult> GetMembers(Guid projectId)
    {
        var members = await _projectService.GetMembersAsync(projectId, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(members));
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteMember(Guid projectId, [FromBody] InviteMemberDto dto)
    {
        await _projectService.InviteMemberAsync(projectId, dto, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(null!, $"Invitation sent to {dto.Email}."));
    }

    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInvitationDto dto)
    {
        await _projectService.AcceptInvitationAsync(dto.Token, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(null!, "Invitation accepted. Welcome to the project!"));
    }

    [HttpPatch("{memberUserId}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid projectId, Guid memberUserId, [FromBody] UpdateProjectMemberRoleDto dto)
    {
        await _projectService.UpdateMemberRoleAsync(projectId, memberUserId, dto, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(null!, "Member role updated."));
    }

    [HttpDelete("{memberUserId}")]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid memberUserId)
    {
        await _projectService.RemoveMemberAsync(projectId, memberUserId, GetCurrentUserId());
        return Ok(ApiResponse<object>.Ok(null!, "Member removed from project."));
    }
}
