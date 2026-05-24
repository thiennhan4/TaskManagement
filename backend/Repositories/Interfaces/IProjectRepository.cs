using TaskHub.backend.Models;

namespace TaskHub.backend.Repositories.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id);
    Task<Project?> GetBySlugAsync(string slug, Guid? workspaceId);
    Task<IEnumerable<Project>> GetWorkspaceProjectsAsync(Guid workspaceId, bool includeArchived = false);
    Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId);
    Task CreateAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(Project project);
    Task<bool> SlugExistsAsync(string slug, Guid? workspaceId);
    
    // Members
    Task AddMemberAsync(ProjectMember member);
    Task RemoveMemberAsync(ProjectMember member);
    Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId);
    Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(Guid projectId);
    
    // Invitations
    Task AddInvitationAsync(ProjectInvitation invitation);
    Task<ProjectInvitation?> GetInvitationByTokenAsync(string token);
    Task UpdateInvitationAsync(ProjectInvitation invitation);
    
    // Activity
    Task AddActivityLogAsync(ProjectActivityLog log);
    Task<IEnumerable<ProjectActivityLog>> GetProjectActivityAsync(Guid projectId, int count = 20);
}
