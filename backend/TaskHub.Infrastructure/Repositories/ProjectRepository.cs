using Microsoft.EntityFrameworkCore;
using TaskHub.Infrastructure.Data;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;

namespace TaskHub.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        return await _context.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project?> GetBySlugAsync(string slug, Guid? workspaceId)
    {
        return await _context.Projects
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.WorkspaceId == workspaceId);
    }

    public async Task<IEnumerable<Project>> GetWorkspaceProjectsAsync(Guid workspaceId, bool includeArchived = false)
    {
        var query = _context.Projects
            .Include(p => p.Owner)
            .Where(p => p.WorkspaceId == workspaceId);

        if (!includeArchived)
        {
            query = query.Where(p => p.ArchivedAt == null);
        }

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId)
    {
        return await _context.ProjectMembers
            .Include(pm => pm.Project)
                .ThenInclude(p => p.Owner)
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.Project)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> CanUserAccessProjectAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        return await _context.Projects
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct)
            || await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, ct);
    }

    public async Task CreateAsync(Project project)
    {
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Project project)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Project project)
    {
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? workspaceId)
    {
        return await _context.Projects.AnyAsync(p => p.Slug == slug && p.WorkspaceId == workspaceId);
    }

    public async Task AddMemberAsync(ProjectMember member)
    {
        await _context.ProjectMembers.AddAsync(member);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(ProjectMember member)
    {
        _context.ProjectMembers.Remove(member);
        await _context.SaveChangesAsync();
    }

    public async Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId)
    {
        return await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }

    public async Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(Guid projectId)
    {
        return await _context.ProjectMembers
            .Include(pm => pm.User)
            .Where(pm => pm.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task AddInvitationAsync(ProjectInvitation invitation)
    {
        await _context.ProjectInvitations.AddAsync(invitation);
        await _context.SaveChangesAsync();
    }

    public async Task<ProjectInvitation?> GetInvitationByTokenAsync(string token)
    {
        return await _context.ProjectInvitations
            .Include(pi => pi.Project)
            .FirstOrDefaultAsync(pi => pi.Token == token);
    }

    public async Task UpdateInvitationAsync(ProjectInvitation invitation)
    {
        _context.ProjectInvitations.Update(invitation);
        await _context.SaveChangesAsync();
    }

    public async Task AddActivityLogAsync(ProjectActivityLog log)
    {
        await _context.ProjectActivityLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProjectActivityLog>> GetProjectActivityAsync(Guid projectId, int count = 20)
    {
        return await _context.ProjectActivityLogs
            .Include(l => l.User)
            .Where(l => l.ProjectId == projectId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}




