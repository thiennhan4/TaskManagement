using Microsoft.EntityFrameworkCore;
using TaskHub.backend.Data;
using TaskHub.backend.Models;
using TaskHub.backend.Repositories.Interfaces;

namespace TaskHub.backend.Repositories;

public class TeamRepository : ITeamRepository
{
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
