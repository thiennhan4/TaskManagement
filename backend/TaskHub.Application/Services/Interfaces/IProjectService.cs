using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services.Interfaces;

public interface IProjectService
{
    Task TransferOwnershipAsync(Guid id, Guid actor, Guid target, CancellationToken ct = default);
    Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto dto, Guid userId, CancellationToken ct = default);
    Task<ProjectResponseDto?> GetProjectByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<ProjectResponseDto?> GetProjectBySlugAsync(string slug, Guid? workspaceId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<ProjectResponseDto>> GetWorkspaceProjectsAsync(Guid workspaceId, Guid userId, bool includeArchived = false, CancellationToken ct = default);
    Task<IEnumerable<ProjectResponseDto>> GetUserProjectsAsync(Guid userId, CancellationToken ct = default);
    Task<ProjectResponseDto> UpdateProjectAsync(Guid id, UpdateProjectDto dto, Guid userId, CancellationToken ct = default);
    Task<ProjectResponseDto> ConvertToTeamAsync(Guid id, Guid teamId, Guid userId, CancellationToken ct = default);
    Task DeleteProjectAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<ProjectResponseDto> ArchiveProjectAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<ProjectResponseDto> RestoreProjectAsync(Guid id, Guid userId, CancellationToken ct = default);
    
    // Member Management
    Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task InviteMemberAsync(Guid projectId, InviteMemberDto dto, Guid userId, CancellationToken ct = default);
    Task AcceptInvitationAsync(string token, Guid userId, Guid? expectedProjectId = null, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid projectId, Guid memberUserId, Guid userId, CancellationToken ct = default);
    Task UpdateMemberRoleAsync(Guid projectId, Guid memberUserId, UpdateProjectMemberRoleDto dto, Guid userId, CancellationToken ct = default);
    
    // Activity
    Task<IEnumerable<ProjectActivityResponseDto>> GetActivityLogsAsync(Guid projectId, Guid userId, CancellationToken ct = default);
}





