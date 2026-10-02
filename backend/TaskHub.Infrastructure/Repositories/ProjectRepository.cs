using TaskHub.Application.Services;
﻿using Microsoft.EntityFrameworkCore;
using TaskHub.Infrastructure.Data;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;

namespace TaskHub.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    public async Task<(int Boards, int Members)> GetCountsAsync(Guid id, CancellationToken ct = default)
    {
        var counts = await _context.Projects.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new { Boards = p.Boards.Count, Members = p.Members.Count }).SingleAsync(ct);
        return (counts.Boards, counts.Members);
    }
    public async Task TransferOwnershipAsync(Guid id, Guid actor, Guid target, CancellationToken ct = default)
    {
        var project=await _context.Projects.SingleAsync(p=>p.Id==id,ct);
        var members=await _context.ProjectMembers.Where(m=>m.ProjectId==id).ToListAsync(ct);
        var next=members.Single(m=>m.UserId==target);
        foreach(var member in members.Where(m=>m.Role==ProjectRole.Owner)) member.Role=ProjectRole.Admin;
        next.Role=ProjectRole.Owner;
        project.OwnerId=target;
        project.UpdatedAt=DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Projects
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Project?> GetBySlugAsync(string slug, Guid? workspaceId, Guid ownerId, CancellationToken ct = default)
    {
        return await _context.Projects
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.WorkspaceId == workspaceId &&
                (workspaceId.HasValue || p.OwnerId == ownerId), cancellationToken: ct);
    }

    public async Task<IEnumerable<Project>> GetWorkspaceProjectsAsync(Guid workspaceId, bool includeArchived = false, CancellationToken ct = default)
    {
        var query = _context.Projects
            .Include(p => p.Owner)
            .Where(p => p.WorkspaceId == workspaceId);

        if (!includeArchived)
        {
            query = query.Where(p => p.ArchivedAt == null);
        }

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public async Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.ProjectMembers
            .Include(pm => pm.Project)
                .ThenInclude(p => p.Owner)
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.Project)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Project>> GetAccessibleProjectsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members)
            .Where(ResourceVisibility.Projects(userId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> CanUserAccessProjectAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        return await _context.Projects.Where(ResourceVisibility.Projects(userId)).AnyAsync(p=>p.Id==projectId,ct);
    }

    public async Task CreateAsync(Project project, CancellationToken ct = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken: ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Project project, CancellationToken ct = default)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Project project, CancellationToken ct = default)
    {
        var attachments = await _context.TaskAttachments.Where(a => a.Task.List.Board.ProjectId == project.Id).ToListAsync(ct);
        foreach (var attachment in attachments)
            _context.OutboxMessages.Add(new OutboxMessage { Kind = "FileCleanup", Payload = System.Text.Json.JsonSerializer.Serialize(new { Key = attachment.FilePath }) });
        // Database cascades remove lists/tasks/comments/time/history after boards are explicitly removed.
        var boards = await _context.Boards.Where(b => b.ProjectId == project.Id).ToListAsync(ct);
        _context.Boards.RemoveRange(boards);
        await _context.SaveChangesAsync(ct);
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? workspaceId, CancellationToken ct = default)
    {
        return await _context.Projects.AnyAsync(p => p.Slug == slug && p.WorkspaceId == workspaceId, cancellationToken: ct);
    }

    public async Task<bool> SlugExistsInScopeAsync(
        string slug,
        Guid? workspaceId,
        Guid ownerId,
        Guid? excludeProjectId = null,
        CancellationToken ct = default)
    {
        return await _context.Projects.AnyAsync(p =>
            p.Slug == slug &&
            (!excludeProjectId.HasValue || p.Id != excludeProjectId.Value) &&
            ((workspaceId.HasValue && p.WorkspaceId == workspaceId) ||
             (!workspaceId.HasValue && p.WorkspaceId == null && p.OwnerId == ownerId)),
            ct);
    }

    public async Task EnsureDefaultBoardStructureAsync(Project project, CancellationToken ct = default)
    {
        var board = await _context.Boards
            .Include(b => b.Lists)
            .FirstOrDefaultAsync(b => b.ProjectId == project.Id && b.Name == "Main Board", ct);

        if (board == null)
        {
            board = new Board
            {
                Name = "Main Board",
                ProjectId = project.Id,
                OwnerId = project.OwnerId,
                Color = project.Color
            };

            _context.Boards.Add(board);
        }

        var defaults = new[]
        {
            new { Name = "To Do", Position = 1, Color = "#9CA3AF" },
            new { Name = "In Progress", Position = 2, Color = "#3B82F6" },
            new { Name = "Done", Position = 3, Color = "#10B981" }
        };

        foreach (var defaultList in defaults)
        {
            if (board.Lists.Any(l => l.Name == defaultList.Name))
                continue;

            _context.Lists.Add(new BoardList
            {
                Name = defaultList.Name,
                Position = defaultList.Position,
                BoardId = board.Id,
                Color = defaultList.Color
            });
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task AddMemberAsync(ProjectMember member, CancellationToken ct = default)
    {
        await _context.ProjectMembers.AddAsync(member, cancellationToken: ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(ProjectMember member, CancellationToken ct = default)
    {
        _context.ProjectMembers.Remove(member);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        return await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, cancellationToken: ct);
    }

    public async Task<ProjectRole?> GetProjectRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var membership = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, ct);

        return membership?.Role;
    }

    public async Task UpdateMemberAsync(ProjectMember member, CancellationToken ct = default)
    {
        _context.ProjectMembers.Update(member);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> IsUserEligibleProjectAssigneeAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        return await _context.Projects.AnyAsync(p =>
            p.Id == projectId && _context.Users.Any(u => u.Id == userId && u.IsActive) &&
            (p.OwnerId == userId ||
             p.Members.Any(pm => pm.UserId == userId) ||
             (p.ProjectType == ProjectType.Team &&
              p.WorkspaceId.HasValue &&
              _context.TeamMembers.Any(tm => tm.TeamId == p.WorkspaceId.Value && tm.UserId == userId))),
            ct);
    }

    public async Task<bool> HasInvalidConversionParticipantsAsync(Guid projectId, Guid teamId, Guid ownerId, CancellationToken ct = default)
    {
        var invalidAssignee = await _context.Tasks.AnyAsync(task =>
            !task.IsDeleted && task.List.Board.ProjectId == projectId &&
            task.AssignedToId.HasValue && task.AssignedToId.Value != ownerId &&
            !_context.TeamMembers.Any(member => member.TeamId == teamId && member.UserId == task.AssignedToId.Value), ct);
        var invalidMember = await _context.ProjectMembers.AnyAsync(member =>
            member.ProjectId == projectId && member.UserId != ownerId &&
            !_context.TeamMembers.Any(teamMember => teamMember.TeamId == teamId && teamMember.UserId == member.UserId), ct);
        return invalidAssignee || invalidMember;
    }

    public async Task ConvertToTeamAsync(Project project, Guid teamId, ProjectMember ownerMembership, ProjectActivityLog activity, CancellationToken ct = default)
    {
        await using var transaction = _context.Database.IsRelational() && _context.Database.CurrentTransaction == null
            ? await _context.Database.BeginTransactionAsync(ct)
            : null;

        var tasks = await _context.Tasks
            .Where(task => task.List.Board.ProjectId == project.Id)
            .ToListAsync(ct);
        foreach (var task in tasks)
            task.TeamId = teamId;

        if (!_context.ProjectMembers.Local.Any(member => member.Id == ownerMembership.Id) &&
            !await _context.ProjectMembers.AnyAsync(member => member.Id == ownerMembership.Id, ct))
            _context.ProjectMembers.Add(ownerMembership);

        _context.ProjectActivityLogs.Add(activity);

        await _context.SaveChangesAsync(ct);
        if (transaction != null)
            await transaction.CommitAsync(ct);
    }

    public async Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _context.ProjectMembers
            .Include(pm => pm.User)
            .Where(pm => pm.ProjectId == projectId)
            .ToListAsync(ct);
    }

    public async Task AddInvitationAsync(ProjectInvitation invitation, CancellationToken ct = default)
    {
        await _context.ProjectInvitations.AddAsync(invitation, cancellationToken: ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<ProjectInvitation?> GetInvitationByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _context.ProjectInvitations
            .Include(pi => pi.Project)
            .FirstOrDefaultAsync(pi => pi.Token == token, cancellationToken: ct);
    }

    public async Task<ProjectInvitation?> GetActiveInvitationAsync(Guid projectId, string inviteeEmail, CancellationToken ct = default)
    {
        var normalizedEmail = inviteeEmail.Trim().ToLowerInvariant();
        if (await EmailComparison.IsCaseInsensitiveAsync(_context, "ProjectInvitations", "InviteeEmail", ct))
            return await _context.ProjectInvitations.FirstOrDefaultAsync(i => i.ProjectId == projectId &&
                i.InviteeEmail == normalizedEmail && !i.IsAccepted && i.ExpiresAt > DateTime.UtcNow, ct);
        return await _context.ProjectInvitations
            .FirstOrDefaultAsync(i =>
                i.ProjectId == projectId &&
                i.InviteeEmail.ToLower() == normalizedEmail &&
                !i.IsAccepted &&
                i.ExpiresAt > DateTime.UtcNow,
                ct);
    }

    public async Task UpdateInvitationAsync(ProjectInvitation invitation, CancellationToken ct = default)
    {
        _context.ProjectInvitations.Update(invitation);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddActivityLogAsync(ProjectActivityLog log, CancellationToken ct = default)
    {
        await _context.ProjectActivityLogs.AddAsync(log, cancellationToken: ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<ProjectActivityLog>> GetProjectActivityAsync(Guid projectId, int count = 20, CancellationToken ct = default)
    {
        return await _context.ProjectActivityLogs
            .Include(l => l.User)
            .Where(l => l.ProjectId == projectId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }
}

