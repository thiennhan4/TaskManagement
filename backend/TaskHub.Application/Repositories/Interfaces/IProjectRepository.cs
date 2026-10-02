using TaskHub.Domain.Entities;

namespace TaskHub.Application.Repositories.Interfaces;

public interface IProjectRepository
{
    Task<(int Boards, int Members)> GetCountsAsync(Guid id, CancellationToken ct = default);
    Task TransferOwnershipAsync(Guid id, Guid actor, Guid target, CancellationToken ct = default);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetBySlugAsync(string slug, Guid? workspaceId, Guid ownerId, CancellationToken ct = default);
    Task<IEnumerable<Project>> GetWorkspaceProjectsAsync(Guid workspaceId, bool includeArchived = false, CancellationToken ct = default);
    Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Project>> GetAccessibleProjectsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> CanUserAccessProjectAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task CreateAsync(Project project, CancellationToken ct = default);
    Task UpdateAsync(Project project, CancellationToken ct = default);
    Task DeleteAsync(Project project, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? workspaceId, CancellationToken ct = default);
    Task<bool> SlugExistsInScopeAsync(string slug, Guid? workspaceId, Guid ownerId, Guid? excludeProjectId = null, CancellationToken ct = default);
    Task EnsureDefaultBoardStructureAsync(Project project, CancellationToken ct = default);
    
    // Members
    Task AddMemberAsync(ProjectMember member, CancellationToken ct = default);
    Task RemoveMemberAsync(ProjectMember member, CancellationToken ct = default);
    Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task<ProjectRole?> GetProjectRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(Guid projectId, CancellationToken ct = default);
    Task UpdateMemberAsync(ProjectMember member, CancellationToken ct = default);
    Task<bool> IsUserEligibleProjectAssigneeAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task<bool> HasInvalidConversionParticipantsAsync(Guid projectId, Guid teamId, Guid ownerId, CancellationToken ct = default);
    Task ConvertToTeamAsync(Project project, Guid teamId, ProjectMember ownerMembership, ProjectActivityLog activity, CancellationToken ct = default);
    
    // Invitations
    Task AddInvitationAsync(ProjectInvitation invitation, CancellationToken ct = default);
    Task<ProjectInvitation?> GetInvitationByTokenAsync(string token, CancellationToken ct = default);
    Task<ProjectInvitation?> GetActiveInvitationAsync(Guid projectId, string inviteeEmail, CancellationToken ct = default);
    Task UpdateInvitationAsync(ProjectInvitation invitation, CancellationToken ct = default);
    
    // Activity
    Task AddActivityLogAsync(ProjectActivityLog log, CancellationToken ct = default);
    Task<IEnumerable<ProjectActivityLog>> GetProjectActivityAsync(Guid projectId, int count = 20, CancellationToken ct = default);
}


