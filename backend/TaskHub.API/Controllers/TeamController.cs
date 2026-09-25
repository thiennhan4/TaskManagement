using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.API.Controllers;

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

    // â”€â”€â”€ GET /api/teams â”€â”€â”€
    [HttpPost("/api/v1/teams/{id}/transfer-ownership")]
    public async Task<IActionResult> TransferOwnership(Guid id, [FromBody] TransferOwnershipDto dto, CancellationToken ct)
    {
        await _teamService.TransferOwnershipAsync(id, GetCurrentUserId(), dto.TargetUserId, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Ownership transferred."));
    }

    [HttpGet]
    public async Task<IActionResult> GetTeams(CancellationToken ct)
    {
        var teams = await _teamService.GetUserTeamsAsync(GetCurrentUserId(), ct);
        return Ok(ApiResponse<IEnumerable<TeamResponseDto>>.Ok(teams));
    }

    // â”€â”€â”€ GET /api/teams/{id} â”€â”€â”€
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTeam(Guid id, CancellationToken ct)
    {
        var team = await _teamService.GetTeamAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TeamDetailResponseDto>.Ok(team));
    }

    // â”€â”€â”€ POST /api/teams â”€â”€â”€
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto, CancellationToken ct)
    {
        var team = await _teamService.CreateTeamAsync(dto, GetCurrentUserId(), ct);
        return CreatedAtAction(nameof(GetTeam), new { id = team.Id },
            ApiResponse<TeamResponseDto>.Ok(team, "Team created."));
    }

    // â”€â”€â”€ PUT /api/teams/{id} â”€â”€â”€
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTeam(Guid id, [FromBody] UpdateTeamDto dto, CancellationToken ct)
    {
        var team = await _teamService.UpdateTeamAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TeamResponseDto>.Ok(team, "Team updated."));
    }

    // â”€â”€â”€ DELETE /api/teams/{id} â”€â”€â”€
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTeam(Guid id, CancellationToken ct)
    {
        await _teamService.DeleteTeamAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Team deleted."));
    }

    // â”€â”€â”€ GET /api/teams/{id}/members â”€â”€â”€
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetTeamMembers(Guid id, CancellationToken ct)
    {
        var members = await _teamService.GetTeamMembersAsync(id, GetCurrentUserId(), ct);
        return Ok(ApiResponse<IEnumerable<TeamMemberResponseDto>>.Ok(members));
    }

    // â”€â”€â”€ POST /api/teams/{id}/members â”€â”€â”€
    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddTeamMember(Guid id, [FromBody] AddTeamMemberDto dto, CancellationToken ct)
    {
        var member = await _teamService.AddTeamMemberAsync(id, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TeamMemberResponseDto>.Ok(member, "Member added."));
    }

    // â”€â”€â”€ DELETE /api/teams/{id}/members/{userId} â”€â”€â”€
    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveTeamMember(Guid id, Guid userId, CancellationToken ct)
    {
        await _teamService.RemoveTeamMemberAsync(id, userId, GetCurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Member removed."));
    }

    // â”€â”€â”€ PUT /api/teams/{id}/members/{userId}/role â”€â”€â”€
    [HttpPut("{id}/members/{userId}/role")]
    public async Task<IActionResult> ChangeMemberRole(Guid id, Guid userId, [FromBody] ChangeTeamRoleDto dto, CancellationToken ct)
    {
        var member = await _teamService.ChangeMemberRoleAsync(id, userId, dto, GetCurrentUserId(), ct);
        return Ok(ApiResponse<TeamMemberResponseDto>.Ok(member, "Role updated."));
    }
}



