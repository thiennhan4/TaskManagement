using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetBySlugAsync(string slug, Guid? workspaceId);
    Task<IEnumerable<Project>> GetWorkspaceProjectsAsync(Guid workspaceId, bool includeArchived = false);
    Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId);
    Task<IEnumerable<Project>> GetAccessibleProjectsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> CanUserAccessProjectAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task CreateAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(Project project);
    Task<bool> SlugExistsAsync(string slug, Guid? workspaceId);
    Task<bool> SlugExistsInScopeAsync(string slug, Guid? workspaceId, Guid ownerId, Guid? excludeProjectId = null, CancellationToken ct = default);
    Task EnsureDefaultBoardStructureAsync(Project project, CancellationToken ct = default);
    
    // Members
    Task AddMemberAsync(ProjectMember member);
    Task RemoveMemberAsync(ProjectMember member);
    Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId);
    Task<ProjectRole?> GetProjectRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(Guid projectId);
    Task UpdateMemberAsync(ProjectMember member, CancellationToken ct = default);
    Task<bool> IsUserEligibleProjectAssigneeAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    
    // Invitations
    Task AddInvitationAsync(ProjectInvitation invitation);
    Task<ProjectInvitation?> GetInvitationByTokenAsync(string token);
    Task<ProjectInvitation?> GetActiveInvitationAsync(Guid projectId, string inviteeEmail, CancellationToken ct = default);
    Task UpdateInvitationAsync(ProjectInvitation invitation);
    
    // Activity
    Task AddActivityLogAsync(ProjectActivityLog log);
    Task<IEnumerable<ProjectActivityLog>> GetProjectActivityAsync(Guid projectId, int count = 20);
}




