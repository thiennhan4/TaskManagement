using TaskHub.Application.DTOs;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Services.Interfaces;

public interface IProjectService
{
    Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto dto, Guid userId);
    Task<ProjectResponseDto?> GetProjectByIdAsync(Guid id, Guid userId);
    Task<ProjectResponseDto?> GetProjectBySlugAsync(string slug, Guid? workspaceId, Guid userId);
    Task<IEnumerable<ProjectResponseDto>> GetWorkspaceProjectsAsync(Guid workspaceId, Guid userId, bool includeArchived = false);
    Task<IEnumerable<ProjectResponseDto>> GetUserProjectsAsync(Guid userId);
    Task<ProjectResponseDto> UpdateProjectAsync(Guid id, UpdateProjectDto dto, Guid userId);
    Task<ProjectResponseDto> ConvertToTeamAsync(Guid id, Guid teamId, Guid userId);
    Task DeleteProjectAsync(Guid id, Guid userId);
    Task<ProjectResponseDto> ArchiveProjectAsync(Guid id, Guid userId);
    Task<ProjectResponseDto> RestoreProjectAsync(Guid id, Guid userId);
    
    // Member Management
    Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(Guid projectId, Guid userId);
    Task InviteMemberAsync(Guid projectId, InviteMemberDto dto, Guid userId);
    Task AcceptInvitationAsync(string token, Guid userId);
    Task RemoveMemberAsync(Guid projectId, Guid memberUserId, Guid userId);
    Task UpdateMemberRoleAsync(Guid projectId, Guid memberUserId, UpdateProjectMemberRoleDto dto, Guid userId);
    
    // Activity
    Task<IEnumerable<ProjectActivityLog>> GetActivityLogsAsync(Guid projectId, Guid userId);
}





