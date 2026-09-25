using Microsoft.EntityFrameworkCore;
using TaskHub.Infrastructure.Data;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;

namespace TaskHub.Infrastructure.Repositories;

public class TeamRepository : ITeamRepository
{
    public Task<bool> HasTasksAsync(Guid teamId, CancellationToken ct) => _context.Tasks.AnyAsync(t=>t.TeamId==teamId,ct);
    public Task<bool> HasProjectsAsync(Guid teamId, CancellationToken ct) => _context.Projects.AnyAsync(p=>p.WorkspaceId==teamId,ct);
    public async Task TransferOwnershipAsync(Guid id, Guid actor, Guid target, CancellationToken ct = default)
    {
        var members=await _context.TeamMembers.Where(m=>m.TeamId==id && (m.UserId==actor || m.UserId==target)).ToListAsync(ct);
        members.Single(m=>m.UserId==target).Role=TeamRole.Owner;
        members.Single(m=>m.UserId==actor).Role=TeamRole.Manager;
        await _context.SaveChangesAsync(ct);
    }
    private readonly AppDbContext _context;

    public TeamRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Team>> GetTeamsByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Teams
            .Include(t => t.CreatedBy)
            .Include(t => t.Members)
            .Where(t => t.Members.Any(m => m.UserId == userId))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Team?> GetTeamByIdAsync(Guid teamId, CancellationToken ct = default)
    {
        return await _context.Teams
            .Include(t => t.CreatedBy)
            .Include(t => t.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);
    }

    public async Task<Team> CreateTeamAsync(Team team, CancellationToken ct = default)
    {
        _context.Teams.Add(team);
        await _context.SaveChangesAsync(ct);
        return team;
    }

    public async Task UpdateTeamAsync(Team team, CancellationToken ct = default)
    {
        _context.Entry(team).State = EntityState.Modified;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteTeamAsync(Guid teamId, CancellationToken ct = default)
    {
        var team = await _context.Teams.FindAsync(new object[] { teamId }, ct);
        if (team != null)
        {
            _context.Teams.Remove(team);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<TeamMember?> GetTeamMemberAsync(Guid teamId, Guid userId, CancellationToken ct = default)
    {
        return await _context.TeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId, ct);
    }

    public async Task AddTeamMemberAsync(TeamMember member, CancellationToken ct = default)
    {
        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveTeamMemberAsync(TeamMember member, CancellationToken ct = default)
    {
        _context.TeamMembers.Remove(member);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateTeamMemberAsync(TeamMember member, CancellationToken ct = default)
    {
        _context.Entry(member).State = EntityState.Modified;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<TeamMember>> GetTeamMembersAsync(Guid teamId, CancellationToken ct = default)
    {
        return await _context.TeamMembers
            .Include(m => m.User)
            .Where(m => m.TeamId == teamId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(ct);
    }
}




