using System.Linq.Expressions;
using TaskHub.Domain.Entities;
namespace TaskHub.Application.Services;

// Query counterparts of PermissionService.View. Project authority takes precedence
// over historical task ownership/assignment; Personal membership rows grant nothing.
public static class ResourceVisibility
{
    public static Expression<Func<Project,bool>> Projects(Guid userId, bool admin=false) => p => admin || p.OwnerId==userId ||
        (p.ProjectType==ProjectType.Team && (p.Members.Any(m=>m.UserId==userId) || (p.Workspace!=null && p.Workspace.Members.Any(m=>m.UserId==userId))));
    public static Expression<Func<TaskItem,bool>> Tasks(Guid userId, bool admin=false) => t => admin ||
        (t.List.Board.Project!=null
            ? t.List.Board.Project.OwnerId==userId || (t.List.Board.Project.ProjectType==ProjectType.Team &&
                (t.List.Board.Project.Members.Any(m=>m.UserId==userId) || (t.List.Board.Project.Workspace!=null && t.List.Board.Project.Workspace.Members.Any(m=>m.UserId==userId))))
            : t.TeamId.HasValue ? t.Team!=null && t.Team.Members.Any(m=>m.UserId==userId)
            : t.OwnerId==userId || t.AssignedToId==userId);
}
