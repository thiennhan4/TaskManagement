using TaskHub.Application.DTOs;
using System.Globalization;
using System.Text;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Exceptions;

namespace TaskHub.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public ProjectService(
        IProjectRepository projectRepository,
        ITeamRepository teamRepository,
        IUserRepository userRepository,
        IPermissionService permissionService,
        IAuditService auditService,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _projectRepository = projectRepository;
        _teamRepository = teamRepository;
        _userRepository = userRepository;
        _permissionService = permissionService;
        _auditService = auditService;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto dto, Guid userId)
    {
        var workspaceId = dto.ProjectType == ProjectType.Personal ? null : dto.WorkspaceId;
        var visibility = dto.ProjectType == ProjectType.Personal ? ProjectVisibility.Private : dto.Visibility;

        if (dto.ProjectType == ProjectType.Team && !workspaceId.HasValue)
        {
            throw new BadRequestException("Team projects require a workspace.");
        }

        if (dto.ProjectType == ProjectType.Team)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new BadRequestException("Project name is required for team projects.");
            }

            var workspace = await _teamRepository.GetTeamByIdAsync(workspaceId!.Value)
                ?? throw new NotFoundException("Workspace", workspaceId.Value);

            await _permissionService.AuthorizeTeamActionAsync(userId, workspace.Id, TeamAction.View);
        }

        var projectName = string.IsNullOrWhiteSpace(dto.Name) ? "Untitled Project" : dto.Name;
        var slug = NormalizeSlug(dto.Slug, projectName);
        var slugExists = await _projectRepository.SlugExistsInScopeAsync(slug, workspaceId, userId);

        if (slugExists)
        {
            throw new BadRequestException("A project with this slug already exists in this workspace.");
        }

        var project = new Project
        {
            Name = projectName,
            Slug = slug,
            Description = dto.Description,
            Emoji = dto.Emoji,
            Color = dto.Color,
            Visibility = visibility,
            ProjectType = dto.ProjectType,
            OwnerId = userId,
            WorkspaceId = workspaceId,
            Status = ProjectStatus.Planning
        };

        await _projectRepository.CreateAsync(project);

        if (project.ProjectType == ProjectType.Team)
        {
            var member = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = userId,
                Role = ProjectRole.Owner
            };
            await _projectRepository.AddMemberAsync(member);
        }

        await LogActivity(project.Id, userId, ProjectActivityAction.Created, $"Created project {project.Name}");

        await _notificationService.CreateNotificationAsync(
            userId,
            "Project created",
            $"Project '{project.Name}' was created successfully.",
            $"/projects"
        );

        await _projectRepository.EnsureDefaultBoardStructureAsync(project);

        return MapToDto(project);
    }

    public async Task<ProjectResponseDto?> GetProjectByIdAsync(Guid id, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null) return null;

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.View);

        return MapToDto(project);
    }

    public async Task<ProjectResponseDto?> GetProjectBySlugAsync(string slug, Guid? workspaceId, Guid userId)
    {
        var project = await _projectRepository.GetBySlugAsync(slug, workspaceId);
        if (project == null) return null;

        return await GetProjectByIdAsync(project.Id, userId);
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetWorkspaceProjectsAsync(Guid workspaceId, Guid userId, bool includeArchived = false)
    {
        await _permissionService.AuthorizeTeamActionAsync(userId, workspaceId, TeamAction.View);

        var projects = await _projectRepository.GetWorkspaceProjectsAsync(workspaceId, includeArchived);
        
        // Filter based on visibility and membership
        var result = new List<ProjectResponseDto>();
        foreach (var p in projects)
        {
            if (p.Visibility == ProjectVisibility.Public || p.Visibility == ProjectVisibility.TeamOnly)
            {
                result.Add(MapToDto(p));
            }
            else
            {
                var member = await _projectRepository.GetMemberAsync(p.Id, userId);
                if (member != null) result.Add(MapToDto(p));
            }
        }
        return result;
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetUserProjectsAsync(Guid userId)
    {
        var projects = await _projectRepository.GetAccessibleProjectsAsync(userId);
        return projects.Select(MapToDto);
    }

    public async Task<ProjectResponseDto> UpdateProjectAsync(Guid id, UpdateProjectDto dto, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null) throw new NotFoundException("Project not found");

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Update);

        if (dto.Name != null) project.Name = dto.Name;
        if (dto.Description != null) project.Description = dto.Description;
        if (dto.Emoji != null) project.Emoji = dto.Emoji;
        if (dto.CoverImageUrl != null) project.CoverImageUrl = dto.CoverImageUrl;
        if (dto.Color != null) project.Color = dto.Color;
        if (dto.Slug != null)
        {
            var normalizedSlug = NormalizeSlug(dto.Slug, project.Name);
            var slugExists = await _projectRepository.SlugExistsInScopeAsync(
                normalizedSlug,
                project.WorkspaceId,
                project.OwnerId,
                id);

            if (slugExists)
            {
                throw new BadRequestException("A project with this slug already exists in this workspace.");
            }

            project.Slug = normalizedSlug;
        }
        
        if (dto.Status.HasValue && project.Status != dto.Status.Value)
        {
            project.Status = dto.Status.Value;
            await LogActivity(id, userId, ProjectActivityAction.StatusChanged, $"Changed status to {dto.Status.Value}");
        }

        if (dto.Visibility.HasValue && project.Visibility != dto.Visibility.Value)
        {
            if (project.OwnerId != userId)
                await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Delete);
            project.Visibility = dto.Visibility.Value;
            await LogActivity(id, userId, ProjectActivityAction.VisibilityChanged, $"Changed visibility to {dto.Visibility.Value}");
        }

        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);
        
        return MapToDto(project);
    }

    public async Task DeleteProjectAsync(Guid id, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null) throw new NotFoundException("Project not found");

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Delete);

        await _projectRepository.DeleteAsync(project);
    }

    public async Task<ProjectResponseDto> ArchiveProjectAsync(Guid id, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null) throw new NotFoundException("Project not found");

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Delete);

        project.ArchivedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);
        await LogActivity(id, userId, ProjectActivityAction.Archived, "Archived the project");

        return MapToDto(project);
    }

    public async Task<ProjectResponseDto> RestoreProjectAsync(Guid id, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null) throw new NotFoundException("Project not found");

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Delete);

        project.ArchivedAt = null;
        await _projectRepository.UpdateAsync(project);
        await LogActivity(id, userId, ProjectActivityAction.Restored, "Restored the project from archive");

        return MapToDto(project);
    }

    public async Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(Guid projectId, Guid userId)
    {
        // Check access
        await GetProjectByIdAsync(projectId, userId);
        
        var members = await _projectRepository.GetProjectMembersAsync(projectId);
        return members.Select(m => new ProjectMemberDto
        {
            Id = m.Id,
            ProjectId = m.ProjectId,
            UserId = m.UserId,
            FullName = m.User.FullName,
            Email = m.User.Email,
            AvatarUrl = m.User.AvatarUrl,
            Role = m.Role,
            JoinedAt = m.JoinedAt
        });
    }

    public async Task InviteMemberAsync(Guid projectId, InviteMemberDto dto, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null) throw new NotFoundException("Project not found");

        if (project.ProjectType == ProjectType.Personal)
        {
            throw new BusinessValidationException("Personal projects do not support member invitations.");
        }

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.ManageMembers);

        var inviteeEmail = dto.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(inviteeEmail))
        {
            throw new BadRequestException("Invitee email is required.");
        }

        var existingUser = await _userRepository.GetByEmailAsync(inviteeEmail);
        if (existingUser != null)
        {
            var existingMember = await _projectRepository.GetMemberAsync(projectId, existingUser.Id);
            if (existingMember != null)
            {
                throw new BadRequestException("This user is already a member of the project.");
            }
        }

        var existingPendingInvite = await _projectRepository.GetActiveInvitationAsync(projectId, inviteeEmail);

        if (existingPendingInvite != null)
        {
            throw new BadRequestException("An active invitation has already been sent to this email.");
        }

        var invitation = new ProjectInvitation
        {
            ProjectId = projectId,
            InvitedByUserId = userId,
            InviteeEmail = inviteeEmail,
            Role = dto.Role,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _projectRepository.AddInvitationAsync(invitation);

        var inviteLink = $"http://localhost:5173/accept-project-invite?projectId={projectId}&token={invitation.Token}";
        var body =
            $"<p>You have been invited to join project: <b>{project.Name}</b>.</p>" +
            $"<p>Role: <b>{dto.Role}</b></p>" +
            $"<p>Click <a href='{inviteLink}'>here</a> to accept the invitation.</p>" +
            $"<p>This invitation will expire on {invitation.ExpiresAt:yyyy-MM-dd HH:mm} UTC.</p>";
        await _emailService.SendEmailAsync(inviteeEmail, $"Project Invitation: {project.Name}", body, true);

        if (existingUser != null)
        {
            await _notificationService.CreateNotificationAsync(
                existingUser.Id,
                "Project invitation",
                $"You were invited to join project: {project.Name} as {dto.Role}.",
                $"/accept-project-invite?projectId={projectId}&token={invitation.Token}"
            );
        }
        
        await LogActivity(projectId, userId, ProjectActivityAction.MemberInvited, $"Invited {inviteeEmail} as {dto.Role}");
    }

    public async Task AcceptInvitationAsync(string token, Guid userId)
    {
        var invitation = await _projectRepository.GetInvitationByTokenAsync(token);
        if (invitation == null || invitation.IsAccepted || invitation.ExpiresAt < DateTime.UtcNow)
        {
            throw new BadRequestException("Invalid or expired invitation token.");
        }

        if (invitation.Project.ProjectType == ProjectType.Personal)
        {
            throw new BusinessValidationException("Personal projects do not support member invitations.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        if (!string.Equals(user.Email, invitation.InviteeEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("This invitation was sent to a different email address.");
        }

        var existingMember = await _projectRepository.GetMemberAsync(invitation.ProjectId, userId);
        if (existingMember != null)
        {
            throw new BadRequestException("You are already a member of this project.");
        }

        invitation.IsAccepted = true;
        invitation.AcceptedAt = DateTime.UtcNow;
        await _projectRepository.UpdateInvitationAsync(invitation);

        var member = new ProjectMember
        {
            ProjectId = invitation.ProjectId,
            UserId = userId,
            Role = invitation.Role
        };

        await _projectRepository.AddMemberAsync(member);
        await LogActivity(invitation.ProjectId, userId, ProjectActivityAction.MemberJoined, "Joined the project via invitation");

        await _notificationService.CreateNotificationAsync(
            invitation.InvitedByUserId,
            "Project invitation accepted",
            $"{user.FullName} accepted your invitation to join project: {invitation.Project.Name}.",
            $"/projects"
        );

        await _notificationService.CreateNotificationAsync(
            userId,
            "Joined project",
            $"You joined project: {invitation.Project.Name}.",
            $"/projects"
        );
    }

    public async Task RemoveMemberAsync(Guid projectId, Guid memberUserId, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null) throw new NotFoundException("Project not found");

        if (project.ProjectType == ProjectType.Personal)
        {
            throw new BusinessValidationException("Personal projects do not support project members.");
        }

        if (userId != memberUserId)
            await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.ManageMembers);

        var targetMember = await _projectRepository.GetMemberAsync(projectId, memberUserId);
        if (targetMember == null) throw new NotFoundException("Member not found");

        if (targetMember.Role == ProjectRole.Owner && userId != memberUserId)
        {
            throw new ForbiddenException("Owner cannot be removed.");
        }

        await _projectRepository.RemoveMemberAsync(targetMember);
        await LogActivity(projectId, userId, ProjectActivityAction.MemberRemoved, $"Removed user {memberUserId}");
    }

    public async Task UpdateMemberRoleAsync(Guid projectId, Guid memberUserId, UpdateProjectMemberRoleDto dto, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null) throw new NotFoundException("Project not found");

        if (project.ProjectType == ProjectType.Personal)
        {
            throw new BusinessValidationException("Personal projects do not support project members.");
        }

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.ManageMembers);

        var targetMember = await _projectRepository.GetMemberAsync(projectId, memberUserId);
        if (targetMember == null) throw new NotFoundException("Member not found");

        if (targetMember.Role == ProjectRole.Owner && dto.Role != ProjectRole.Owner)
        {
            throw new ForbiddenException("Cannot demote the owner. Transfer ownership instead.");
        }

        targetMember.Role = dto.Role;
        await _projectRepository.UpdateMemberAsync(targetMember);
        
        await LogActivity(projectId, userId, ProjectActivityAction.MemberRoleChanged, $"Updated role of user {memberUserId} to {dto.Role}");
    }

    public async Task<IEnumerable<ProjectActivityLog>> GetActivityLogsAsync(Guid projectId, Guid userId)
    {
        await GetProjectByIdAsync(projectId, userId);
        return await _projectRepository.GetProjectActivityAsync(projectId);
    }

    private async Task LogActivity(Guid projectId, Guid userId, ProjectActivityAction action, string description)
    {
        var log = new ProjectActivityLog
        {
            ProjectId = projectId,
            UserId = userId,
            Action = action,
            Description = description
        };
        await _projectRepository.AddActivityLogAsync(log);
    }

    private ProjectResponseDto MapToDto(Project p)
    {
        return new ProjectResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Description = p.Description,
            Emoji = p.Emoji,
            CoverImageUrl = p.CoverImageUrl,
            Color = p.Color,
            Status = p.Status,
            Visibility = p.Visibility,
            ProjectType = p.ProjectType,
            OwnerId = p.OwnerId,
            OwnerName = p.Owner?.FullName ?? "Unknown",
            WorkspaceId = p.WorkspaceId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            ArchivedAt = p.ArchivedAt,
            IsArchived = p.IsArchived,
            MemberCount = p.Members?.Count ?? 0,
            BoardCount = p.Boards?.Count ?? 0
        };
    }

    private static string NormalizeSlug(string? rawSlug, string fallbackName)
    {
        var source = string.IsNullOrWhiteSpace(rawSlug) ? fallbackName : rawSlug;
        var normalized = RemoveDiacritics(source).Trim().ToLowerInvariant();
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, "[^a-z0-9]+", "-").Trim('-');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BadRequestException("Project slug is invalid.");
        }

        return normalized;
    }

    private static string RemoveDiacritics(string value)
    {
        var normalizedString = value.Replace((char)0x0111, 'd').Replace((char)0x0110, 'D').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}






