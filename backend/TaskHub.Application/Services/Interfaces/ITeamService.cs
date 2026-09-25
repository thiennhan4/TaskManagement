using TaskHub.Application.DTOs;

namespace TaskHub.Application.Services.Interfaces;

public interface ITeamService
{
    Task TransferOwnershipAsync(Guid id, Guid actor, Guid target, CancellationToken ct = default);
    Task<IEnumerable<TeamResponseDto>> GetUserTeamsAsync(Guid userId, CancellationToken ct = default);
    Task<TeamDetailResponseDto> GetTeamAsync(Guid teamId, Guid userId, CancellationToken ct = default);
    Task<TeamResponseDto> CreateTeamAsync(CreateTeamDto dto, Guid userId, CancellationToken ct = default);
    Task<TeamResponseDto> UpdateTeamAsync(Guid teamId, UpdateTeamDto dto, Guid userId, CancellationToken ct = default);
    Task DeleteTeamAsync(Guid teamId, Guid userId, CancellationToken ct = default);

    Task<IEnumerable<TeamMemberResponseDto>> GetTeamMembersAsync(Guid teamId, Guid userId, CancellationToken ct = default);
    Task<TeamMemberResponseDto> AddTeamMemberAsync(Guid teamId, AddTeamMemberDto dto, Guid userId, CancellationToken ct = default);
    Task RemoveTeamMemberAsync(Guid teamId, Guid targetUserId, Guid currentUserId, CancellationToken ct = default);
    Task<TeamMemberResponseDto> ChangeMemberRoleAsync(Guid teamId, Guid targetUserId, ChangeTeamRoleDto dto, Guid currentUserId, CancellationToken ct = default);
}





