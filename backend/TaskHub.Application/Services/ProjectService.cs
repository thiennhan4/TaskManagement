using TaskHub.Application.DTOs;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using TaskHub.Application.Hubs;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Exceptions;

namespace TaskHub.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IMutationRunner _mutations;
    private readonly DurableDelivery _delivery;
    private readonly IProjectRepository _projectRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IProtectedHubContext _hubContext;

    public ProjectService(
        IProjectRepository projectRepository,
        ITeamRepository teamRepository,
        IUserRepository userRepository,
        IPermissionService permissionService,
        IAuditService auditService,
        IEmailService emailService,
        INotificationService notificationService,
        IProtectedHubContext hubContext, IMutationRunner mutations, DurableDelivery delivery)
    {
        _mutations = mutations;
        _delivery = delivery;
        _projectRepository = projectRepository;
        _teamRepository = teamRepository;
        _userRepository = userRepository;
        _permissionService = permissionService;
        _auditService = auditService;
        _emailService = emailService;
        _notificationService = notificationService;
        _hubContext = hubContext;
    }

    public Task TransferOwnershipAsync(Guid id, Guid actor, Guid target, CancellationToken ct = default) =>
        _mutations.RunAsync(() => TransferOwnershipAsyncCore(id, actor, target, ct), ct);

    private async Task TransferOwnershipAsyncCore(Guid id, Guid actor, Guid target, CancellationToken ct = default)
    {
        var project=await _projectRepository.GetByIdAsync(id,ct) ?? throw new NotFoundException("Project",id);
        await _permissionService.AuthorizeProjectTransferAsync(actor,project,ct);
        if(actor==target) throw new BusinessValidationException("Choose a different owner.");
        var member=await _projectRepository.GetMemberAsync(id,target, ct: ct) ?? throw new BusinessValidationException("New owner must already be a project member.");
        var user=await _userRepository.GetByIdAsync(target,ct);
        if(user?.IsActive!=true) throw new BusinessValidationException("New owner must be active.");
        await _projectRepository.TransferOwnershipAsync(id,actor,target,ct);
        await _delivery.EnqueueAsync("Revalidate", new { }, () => _hubContext.RevalidateAsync(), ct);
    }
    public Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => CreateProjectAsyncCore(dto, userId, ct), ct);

    private async Task<ProjectResponseDto> CreateProjectAsyncCore(CreateProjectDto dto, Guid userId, CancellationToken ct = default)
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

            var workspace = await _teamRepository.GetTeamByIdAsync(workspaceId!.Value, ct: ct)
                ?? throw new NotFoundException("Workspace", workspaceId.Value);

            await _permissionService.AuthorizeTeamActionAsync(userId, workspace.Id, TeamAction.View, ct: ct);
        }

        var projectName = string.IsNullOrWhiteSpace(dto.Name) ? "Untitled Project" : dto.Name;
        var slug = NormalizeSlug(dto.Slug, projectName);
        var slugExists = await _projectRepository.SlugExistsInScopeAsync(slug, workspaceId, userId, ct: ct);

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

        await _projectRepository.CreateAsync(project, ct: ct);

        if (project.ProjectType == ProjectType.Team)
        {
            var member = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = userId,
                Role = ProjectRole.Owner
            };
            await _projectRepository.AddMemberAsync(member, ct: ct);
        }

        await _projectRepository.EnsureDefaultBoardStructureAsync(project, ct: ct);

        await _notificationService.CreateNotificationAsync(
            userId,
            "Project created",
            $"Project '{project.Name}' was created successfully.",
            $"/projects"
        , ct: ct);

        await LogActivity(project.Id, userId, ProjectActivityAction.Created, $"Created project {project.Name}", ct: ct);

        return await MapToDtoAsync(project, ct);
    }

    public async Task<ProjectResponseDto?> GetProjectByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(id, ct: ct);
        if (project == null) return null;

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.View, ct: ct);

        return await MapToDtoAsync(project, ct);
    }

    public async Task<ProjectResponseDto?> GetProjectBySlugAsync(string slug, Guid? workspaceId, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetBySlugAsync(slug, workspaceId, userId, ct: ct);
        if (project == null) return null;

        return await GetProjectByIdAsync(project.Id, userId, ct);
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetWorkspaceProjectsAsync(Guid workspaceId, Guid userId, bool includeArchived = false, CancellationToken ct = default)
    {
        await _permissionService.AuthorizeTeamActionAsync(userId, workspaceId, TeamAction.View, ct: ct);

        var projects = await _projectRepository.GetWorkspaceProjectsAsync(workspaceId, includeArchived, ct: ct);
        
        var result = new List<ProjectResponseDto>();
        foreach (var p in projects)
        {
            try { await _permissionService.AuthorizeProjectActionAsync(userId, p, ProjectAction.View, ct: ct); }
            catch (ForbiddenException) { continue; }
            result.Add(await MapToDtoAsync(p, ct));
        }
        return result;
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetUserProjectsAsync(Guid userId, CancellationToken ct = default)
    {
        var projects = await _projectRepository.GetAccessibleProjectsAsync(userId, ct: ct);
        var result = new List<ProjectResponseDto>(); foreach (var project in projects) result.Add(await MapToDtoAsync(project, ct)); return result;
    }

    public Task<ProjectResponseDto> UpdateProjectAsync(Guid id, UpdateProjectDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => UpdateProjectAsyncCore(id, dto, userId, ct), ct);

    private async Task<ProjectResponseDto> UpdateProjectAsyncCore(Guid id, UpdateProjectDto dto, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(id, ct: ct);
        if (project == null) throw new NotFoundException("Project not found");

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Update, ct: ct);

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
                id, ct: ct);

            if (slugExists)
            {
                throw new BadRequestException("A project with this slug already exists in this workspace.");
            }

            project.Slug = normalizedSlug;
        }
        
        var newStatus = dto.Status.GetValueOrDefault();
        var statusChanged = dto.Status.HasValue && project.Status != newStatus;
        if (statusChanged)
        {
            project.Status = newStatus;
        }

        var newVisibility = dto.Visibility.GetValueOrDefault();
        var visibilityChanged = dto.Visibility.HasValue && project.Visibility != newVisibility;
        if (visibilityChanged)
        {
            if (project.OwnerId != userId)
                await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Delete, ct: ct);
            project.Visibility = newVisibility;
        }

        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project, ct: ct);
        if (statusChanged)
            await LogActivity(id, userId, ProjectActivityAction.StatusChanged, $"Changed status to {newStatus}", ct: ct);
        if (visibilityChanged)
            await LogActivity(id, userId, ProjectActivityAction.VisibilityChanged, $"Changed visibility to {newVisibility}", ct: ct);
        
        return await MapToDtoAsync(project, ct);
    }

    public Task<ProjectResponseDto> ConvertToTeamAsync(Guid id, Guid teamId, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => ConvertToTeamAsyncCore(id, teamId, userId, ct), ct);

    private async Task<ProjectResponseDto> ConvertToTeamAsyncCore(Guid id, Guid teamId, Guid userId, CancellationToken ct = default)
    {
        if (teamId == Guid.Empty)
            throw new BadRequestException("Target team is required.");

        var project = await _projectRepository.GetByIdAsync(id, ct: ct)
            ?? throw new NotFoundException("Project", id);

        if (project.ProjectType != ProjectType.Personal || project.WorkspaceId.HasValue)
            throw new BusinessValidationException("Only a Personal project can be converted to a Team project.");

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Delete, ct: ct);

        if (project.OwnerId != userId)
            throw new ForbiddenException("Only the Personal project owner can convert it.");

        _ = await _teamRepository.GetTeamByIdAsync(teamId, ct: ct)
            ?? throw new NotFoundException("Team", teamId);
        await _permissionService.AuthorizeTeamActionAsync(userId, teamId, TeamAction.View, ct: ct);

        if (await _projectRepository.SlugExistsInScopeAsync(project.Slug, teamId, userId, id, ct: ct))
            throw new BusinessValidationException("A project with this slug already exists in the target team.");

        if (await _projectRepository.HasInvalidConversionParticipantsAsync(id, teamId, userId, ct: ct))
            throw new BusinessValidationException("Existing project members or task assignees must belong to the target team.");

        project.ProjectType = ProjectType.Team;
        project.WorkspaceId = teamId;
        project.Visibility = ProjectVisibility.TeamOnly;
        project.UpdatedAt = DateTime.UtcNow;
        var ownerMembership = project.Members.FirstOrDefault(member => member.UserId == userId)
            ?? new ProjectMember { ProjectId = id, UserId = userId };
        ownerMembership.Role = ProjectRole.Owner;
        var activity = new ProjectActivityLog
        {
            ProjectId = id,
            UserId = userId,
            Action = ProjectActivityAction.Updated,
            Description = "Converted project to a team project"
        };

        await _projectRepository.ConvertToTeamAsync(project, teamId, ownerMembership, activity, ct: ct);
        await _delivery.EnqueueAsync("Realtime", new { Group = $"team_{teamId}", Event = "ProjectActivity", Arguments = new object?[] {  new ProjectActivityEventDto
        {
            ProjectId = project.Id,
            EventType = "ConvertedToTeam",
            CreatedAt = project.UpdatedAt!.Value
        } } }, () => _hubContext.Clients.Group($"team_{teamId}").SendAsync("ProjectActivity", new ProjectActivityEventDto
        {
            ProjectId = project.Id,
            EventType = "ConvertedToTeam",
            CreatedAt = project.UpdatedAt!.Value
        }), ct);
        return await MapToDtoAsync(project, ct);
    }

    public Task DeleteProjectAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => DeleteProjectAsyncCore(id, userId, ct), ct);

    private async Task DeleteProjectAsyncCore(Guid id, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(id, ct: ct);
        if (project == null) throw new NotFoundException("Project not found");

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Delete, ct: ct);

        await _projectRepository.DeleteAsync(project, ct: ct);
    }

    public Task<ProjectResponseDto> ArchiveProjectAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => ArchiveProjectAsyncCore(id, userId, ct), ct);

    private async Task<ProjectResponseDto> ArchiveProjectAsyncCore(Guid id, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(id, ct: ct);
        if (project == null) throw new NotFoundException("Project not found");

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Delete, ct: ct);

        project.ArchivedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project, ct: ct);
        await LogActivity(id, userId, ProjectActivityAction.Archived, "Archived the project", ct: ct);

        return await MapToDtoAsync(project, ct);
    }

    public Task<ProjectResponseDto> RestoreProjectAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => RestoreProjectAsyncCore(id, userId, ct), ct);

    private async Task<ProjectResponseDto> RestoreProjectAsyncCore(Guid id, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(id, ct: ct);
        if (project == null) throw new NotFoundException("Project not found");

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.Delete, ct: ct);

        project.ArchivedAt = null;
        await _projectRepository.UpdateAsync(project, ct: ct);
        await LogActivity(id, userId, ProjectActivityAction.Restored, "Restored the project from archive", ct: ct);

        return await MapToDtoAsync(project, ct);
    }

    public async Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        // Check access
        await GetProjectByIdAsync(projectId, userId, ct);
        
        var members = await _projectRepository.GetProjectMembersAsync(projectId, ct: ct);
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

    public Task InviteMemberAsync(Guid projectId, InviteMemberDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => InviteMemberAsyncCore(projectId, dto, userId, ct), ct);

    private async Task InviteMemberAsyncCore(Guid projectId, InviteMemberDto dto, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, ct: ct);
        if (project == null) throw new NotFoundException("Project not found");

        if (project.ProjectType == ProjectType.Personal)
        {
            throw new BusinessValidationException("Personal projects do not support member invitations.");
        }

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.ManageMembers, ct: ct);

        await _permissionService.AuthorizeProjectRoleChangeAsync(userId, project, null, dto.Role, ct: ct);
        var inviteeEmail = dto.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(inviteeEmail))
        {
            throw new BadRequestException("Invitee email is required.");
        }

        var existingUser = await _userRepository.GetByEmailAsync(inviteeEmail, ct: ct);
        if (existingUser != null)
        {
            var existingMember = await _projectRepository.GetMemberAsync(projectId, existingUser.Id, ct: ct);
            if (existingMember != null)
            {
                throw new BadRequestException("This user is already a member of the project.");
            }
        }

        var existingPendingInvite = await _projectRepository.GetActiveInvitationAsync(projectId, inviteeEmail, ct: ct);

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

        await _projectRepository.AddInvitationAsync(invitation, ct: ct);

        var inviteLink = $"http://localhost:5173/accept-project-invite?projectId={projectId}&token={invitation.Token}";
        var body =
            $"<p>You have been invited to join project: <b>{project.Name}</b>.</p>" +
            $"<p>Role: <b>{dto.Role}</b></p>" +
            $"<p>Click <a href='{inviteLink}'>here</a> to accept the invitation.</p>" +
            $"<p>This invitation will expire on {invitation.ExpiresAt:yyyy-MM-dd HH:mm} UTC.</p>";
        await _delivery.EnqueueAsync("Email", new object?[] { inviteeEmail, $"Project Invitation: {project.Name}", body, true }, () => _emailService.SendEmailAsync(inviteeEmail, $"Project Invitation: {project.Name}", body, true), ct);

        if (existingUser != null)
        {
            await _notificationService.CreateNotificationAsync(
                existingUser.Id,
                "Project invitation",
                $"You were invited to join project: {project.Name} as {dto.Role}.",
                $"/accept-project-invite?projectId={projectId}&token={invitation.Token}", ct
            );
        }
        
        await LogActivity(projectId, userId, ProjectActivityAction.MemberInvited, $"Invited {inviteeEmail} as {dto.Role}", ct: ct);
    }

    public Task AcceptInvitationAsync(string token, Guid userId, Guid? expectedProjectId = null, CancellationToken ct = default) =>
        _mutations.RunAsync(() => AcceptInvitationAsyncCore(token, userId, expectedProjectId, ct), ct);

    private async Task AcceptInvitationAsyncCore(string token, Guid userId, Guid? expectedProjectId = null, CancellationToken ct = default)
    {
        var invitation = await _projectRepository.GetInvitationByTokenAsync(token, ct: ct);
        if (invitation == null || invitation.IsAccepted || invitation.ExpiresAt < DateTime.UtcNow)
        {
            throw new BadRequestException("Invalid or expired invitation token.");
        }

        if (expectedProjectId.HasValue && invitation.ProjectId != expectedProjectId.Value)
            throw new NotFoundException("Invitation does not belong to this project.");
        await _permissionService.AuthorizeProjectRoleChangeAsync(invitation.InvitedByUserId, invitation.Project, null, invitation.Role, ct: ct);
        if (invitation.Project.ProjectType == ProjectType.Personal)
        {
            throw new BusinessValidationException("Personal projects do not support member invitations.");
        }

        var user = await _userRepository.GetByIdAsync(userId, ct: ct);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        if (!string.Equals(user.Email, invitation.InviteeEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("This invitation was sent to a different email address.");
        }

        var existingMember = await _projectRepository.GetMemberAsync(invitation.ProjectId, userId, ct: ct);
        if (existingMember != null)
        {
            throw new BadRequestException("You are already a member of this project.");
        }

        invitation.IsAccepted = true;
        invitation.AcceptedAt = DateTime.UtcNow;
        await _projectRepository.UpdateInvitationAsync(invitation, ct: ct);

        var member = new ProjectMember
        {
            ProjectId = invitation.ProjectId,
            UserId = userId,
            Role = invitation.Role
        };

        await _projectRepository.AddMemberAsync(member, ct: ct);
        await LogActivity(invitation.ProjectId, userId, ProjectActivityAction.MemberJoined, "Joined the project via invitation", ct: ct);

        await _notificationService.CreateNotificationAsync(
            invitation.InvitedByUserId,
            "Project invitation accepted",
            $"{user.FullName} accepted your invitation to join project: {invitation.Project.Name}.",
            $"/projects", ct
        );

        await _notificationService.CreateNotificationAsync(
            userId,
            "Joined project",
            $"You joined project: {invitation.Project.Name}.",
            $"/projects", ct
        );
    }

    public Task RemoveMemberAsync(Guid projectId, Guid memberUserId, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => RemoveMemberAsyncCore(projectId, memberUserId, userId, ct), ct);

    private async Task RemoveMemberAsyncCore(Guid projectId, Guid memberUserId, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, ct: ct);
        if (project == null) throw new NotFoundException("Project not found");

        if (project.ProjectType == ProjectType.Personal)
        {
            throw new BusinessValidationException("Personal projects do not support project members.");
        }

        if (userId != memberUserId)
            await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.ManageMembers, ct: ct);

        var targetMember = await _projectRepository.GetMemberAsync(projectId, memberUserId, ct: ct);
        if (targetMember == null) throw new NotFoundException("Member not found");

        if (targetMember.Role == ProjectRole.Owner || project.OwnerId == memberUserId)
        {
            throw new ForbiddenException("Owner cannot be removed.");
        }

        await _projectRepository.RemoveMemberAsync(targetMember, ct: ct);
        await _delivery.EnqueueAsync("Revalidate", new { }, () => _hubContext.RevalidateAsync(), ct);
        await LogActivity(projectId, userId, ProjectActivityAction.MemberRemoved, $"Removed user {memberUserId}", ct: ct);
    }

    public Task UpdateMemberRoleAsync(Guid projectId, Guid memberUserId, UpdateProjectMemberRoleDto dto, Guid userId, CancellationToken ct = default) =>
        _mutations.RunAsync(() => UpdateMemberRoleAsyncCore(projectId, memberUserId, dto, userId, ct), ct);

    private async Task UpdateMemberRoleAsyncCore(Guid projectId, Guid memberUserId, UpdateProjectMemberRoleDto dto, Guid userId, CancellationToken ct = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, ct: ct);
        if (project == null) throw new NotFoundException("Project not found");

        if (project.ProjectType == ProjectType.Personal)
        {
            throw new BusinessValidationException("Personal projects do not support project members.");
        }

        await _permissionService.AuthorizeProjectActionAsync(userId, project, ProjectAction.ManageMembers, ct: ct);

        var targetMember = await _projectRepository.GetMemberAsync(projectId, memberUserId, ct: ct);
        if (targetMember == null) throw new NotFoundException("Member not found");

        if (targetMember.Role == ProjectRole.Owner && dto.Role != ProjectRole.Owner)
        {
            throw new ForbiddenException("Cannot demote the owner. Transfer ownership instead.");
        }

        await _permissionService.AuthorizeProjectRoleChangeAsync(userId, project, targetMember.Role, dto.Role, ct: ct);
        targetMember.Role = dto.Role;
        await _projectRepository.UpdateMemberAsync(targetMember, ct: ct);
        await _delivery.EnqueueAsync("Revalidate", new { }, () => _hubContext.RevalidateAsync(), ct);
        
        await LogActivity(projectId, userId, ProjectActivityAction.MemberRoleChanged, $"Updated role of user {memberUserId} to {dto.Role}", ct: ct);
    }

    public async Task<IEnumerable<ProjectActivityResponseDto>> GetActivityLogsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        await GetProjectByIdAsync(projectId, userId, ct);
        return (await _projectRepository.GetProjectActivityAsync(projectId, ct: ct)).Select(log => new ProjectActivityResponseDto { Id = log.Id, ProjectId = log.ProjectId, UserId = log.UserId, Action = log.Action, Description = log.Description, CreatedAt = log.CreatedAt, User = new UserSummaryDto { Id = log.UserId, FullName = log.User.FullName, AvatarUrl = log.User.AvatarUrl } });
    }

    private async Task LogActivity(Guid projectId, Guid userId, ProjectActivityAction action, string description, CancellationToken ct = default)
    {
        var log = new ProjectActivityLog
        {
            ProjectId = projectId,
            UserId = userId,
            Action = action,
            Description = description
        };
        await _projectRepository.AddActivityLogAsync(log, ct: ct);
        var project = await _projectRepository.GetByIdAsync(projectId, ct: ct);
        var payload = new ProjectActivityEventDto
        {
            ProjectId = projectId,
            EventType = action.ToString(),
            CreatedAt = log.CreatedAt
        };
        await _delivery.EnqueueAsync("Realtime", new { Group = $"project_{projectId}", Event = "ProjectActivity", Arguments = new object?[] {  payload } }, () => _hubContext.Clients.Group($"project_{projectId}").SendAsync("ProjectActivity", payload), ct);
        if (project?.WorkspaceId is Guid teamId)
            await _delivery.EnqueueAsync("Realtime", new { Group = $"team_{teamId}", Event = "ProjectActivity", Arguments = new object?[] {  payload } }, () => _hubContext.Clients.Group($"team_{teamId}").SendAsync("ProjectActivity", payload), ct);
    }

    private async Task<ProjectResponseDto> MapToDtoAsync(Project p, CancellationToken ct)
    {
        var counts = await _projectRepository.GetCountsAsync(p.Id, ct);
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
            MemberCount = counts.Members,
            BoardCount = counts.Boards
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



