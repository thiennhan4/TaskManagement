using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.backend.DTOs;
using TaskHub.backend.Services.Interfaces;

namespace TaskHub.backend.Controllers;

[Route("api/teams")]
[ApiController]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly ITeamService _teamService;

    public TeamController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── GET /api/teams ───
    [HttpGet]
    public async Task<IActionResult> GetTeams(CancellationToken ct)
    {
        var teams = await _teamService.GetUserTeamsAsync(GetCurrentUserId(), ct);
        return Ok(ApiResponse<IEnumerable<TeamResponseDto>>.Ok(teams));
    }

    // ─── GET /api/teams/{id} ───
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTeam(Guid id, CancellationToken ct)
    {
        var team = await _teamService.GetTeamAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TeamDetailResponseDto>.Ok(team));
    }

    // ─── POST /api/teams ───
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto, CancellationToken ct)
    {
        var team = await _teamService.CreateTeamAsync(dto, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetTeam), new { id = team.Id },
            ApiResponse<TeamResponseDto>.Ok(team, "Team created."));
    }

    // ─── PUT /api/teams/{id} ───
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTeam(Guid id, [FromBody] UpdateTeamDto dto, CancellationToken ct)
    {
        var team = await _teamService.UpdateTeamAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TeamResponseDto>.Ok(team, "Team updated."));
    }

    // ─── DELETE /api/teams/{id} ───
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTeam(Guid id, CancellationToken ct)
    {
        await _teamService.DeleteTeamAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Team deleted."));
    }

    // ─── GET /api/teams/{id}/members ───
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetTeamMembers(Guid id, CancellationToken ct)
    {
        var members = await _teamService.GetTeamMembersAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<IEnumerable<TeamMemberResponseDto>>.Ok(members));
    }

    // ─── POST /api/teams/{id}/members ───
    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddTeamMember(Guid id, [FromBody] AddTeamMemberDto dto, CancellationToken ct)
    {
        var member = await _teamService.AddTeamMemberAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TeamMemberResponseDto>.Ok(member, "Member added."));
    }

    // ─── DELETE /api/teams/{id}/members/{userId} ───
    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveTeamMember(Guid id, Guid userId, CancellationToken ct)
    {
        await _teamService.RemoveTeamMemberAsync(id, userId, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Member removed."));
    }

    // ─── PUT /api/teams/{id}/members/{userId}/role ───
    [HttpPut("{id}/members/{userId}/role")]
    public async Task<IActionResult> ChangeMemberRole(Guid id, Guid userId, [FromBody] ChangeTeamRoleDto dto, CancellationToken ct)
    {
        var member = await _teamService.ChangeMemberRoleAsync(id, userId, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TeamMemberResponseDto>.Ok(member, "Role updated."));
    }
}
