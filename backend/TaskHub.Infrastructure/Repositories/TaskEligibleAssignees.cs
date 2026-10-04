using Microsoft.EntityFrameworkCore;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Infrastructure.Repositories;

public partial class TaskItemRepository
{
    public async Task<PagedResult<EligibleAssigneeDto>> GetEligibleAssigneesAsync(TaskItem task, int page, int pageSize, CancellationToken ct = default)
    {
        var project = task.List?.Board?.Project;
        var owner = project?.OwnerId ?? task.List?.Board?.OwnerId ?? task.OwnerId;
        var teamId = project?.ProjectType == ProjectType.Team ? project.WorkspaceId : task.TeamId;
        var projectId = project?.ProjectType == ProjectType.Team ? project.Id : (Guid?)null;
        var users = _context.Users.AsNoTracking().Where(user => user.IsActive &&
            (user.Id == owner ||
             (projectId.HasValue && _context.ProjectMembers.Any(member => member.ProjectId == projectId.Value && member.UserId == user.Id)) ||
             (teamId.HasValue && _context.TeamMembers.Any(member => member.TeamId == teamId.Value && member.UserId == user.Id))));
        var total = await users.CountAsync(ct);
        return new PagedResult<EligibleAssigneeDto>
        {
            Items = await users.OrderBy(user => user.FullName).ThenBy(user => user.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(user => new EligibleAssigneeDto { Id = user.Id, FullName = user.FullName }).ToListAsync(ct),
            TotalItems = total, Page = page, PageSize = pageSize
        };
    }
}
