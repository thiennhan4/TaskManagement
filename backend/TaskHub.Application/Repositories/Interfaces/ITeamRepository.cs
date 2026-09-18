using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface ITeamRepository
{
    Task<IEnumerable<Team>> GetTeamsByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Team?> GetTeamByIdAsync(Guid teamId, CancellationToken ct = default);
    Task<Team> CreateTeamAsync(Team team, CancellationToken ct = default);
    Task UpdateTeamAsync(Team team, CancellationToken ct = default);
    Task DeleteTeamAsync(Guid teamId, CancellationToken ct = default);

    Task<TeamMember?> GetTeamMemberAsync(Guid teamId, Guid userId, CancellationToken ct = default);
    Task AddTeamMemberAsync(TeamMember member, CancellationToken ct = default);
    Task RemoveTeamMemberAsync(TeamMember member, CancellationToken ct = default);
    Task UpdateTeamMemberAsync(TeamMember member, CancellationToken ct = default);
    Task<IEnumerable<TeamMember>> GetTeamMembersAsync(Guid teamId, CancellationToken ct = default);
}




