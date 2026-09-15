using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Data;
using TaskHub.Application.DTOs;
using TaskHub.Domain.Exceptions;
using TaskHub.Domain.Entities;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.Application.Services;

public class TeamService : ITeamService
{
    private readonly ITeamRepository _teamRepository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly IAppDbContext _context;

    public TeamService(
        ITeamRepository teamRepository,
        IPermissionService permissionService,
        IAuditService auditService,
        IAppDbContext context)
    {
        _teamRepository = teamRepository;
        _permissionService = permissionService;
        _auditService = auditService;
        _context = context;
    }

    public async Task<IEnumerable<TeamResponseDto>> GetUserTeamsAsync(Guid userId, CancellationToken ct = default)
    {
        var teams = await _teamRepository.GetTeamsByUserIdAsync(userId, ct);
        var result = new List<TeamResponseDto>();

        foreach (var team in teams)
        {
            var role = team.Members.FirstOrDefault(m => m.UserId == userId)?.Role;
            result.Add(new TeamResponseDto
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                CreatedById = team.CreatedById,
                CreatedByName = team.CreatedBy?.FullName ?? "Unknown",
                CreatedAt = team.CreatedAt,
                MemberCount = team.Members.Count,
                CurrentUserRole = role
            });
        }

        return result;
    }

    public async Task<TeamDetailResponseDto> GetTeamAsync(Guid teamId, Guid userId, CancellationToken ct = default)
    {
        await _permissionService.AuthorizeTeamActionAsync(userId, teamId, TeamAction.View, ct);

        var team = await _teamRepository.GetTeamByIdAsync(teamId, ct)
            ?? throw new NotFoundException("Team", teamId);

        return new TeamDetailResponseDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            CreatedById = team.CreatedById,
            CreatedByName = team.CreatedBy?.FullName ?? "Unknown",
            CreatedAt = team.CreatedAt,
            Members = team.Members.Select(m => new TeamMemberResponseDto
            {
                Id = m.Id,
                UserId = m.UserId,
                FullName = m.User.FullName,
                Email = m.User.Email,
                AvatarUrl = m.User.AvatarUrl,
                Role = m.Role,
                JoinedAt = m.JoinedAt
            }).ToList()
        };
    }

    public async Task<TeamResponseDto> CreateTeamAsync(CreateTeamDto dto, Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("User", userId);

        var team = new Team
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        // Creator is automatically the Owner
        team.Members.Add(new TeamMember
        {
            UserId = userId,
            Role = TeamRole.Owner,
            JoinedAt = DateTime.UtcNow
        });

        await _teamRepository.CreateTeamAsync(team, ct);
        await _auditService.LogAsync(userId, "CreateTeam", "Team", team.Id, new { team.Name }, ct);

        return new TeamResponseDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            CreatedById = team.CreatedById,
            CreatedByName = user.FullName,
            CreatedAt = team.CreatedAt,
            MemberCount = 1,
            CurrentUserRole = TeamRole.Owner
        };
    }

    public async Task<TeamResponseDto> UpdateTeamAsync(Guid teamId, UpdateTeamDto dto, Guid userId, CancellationToken ct = default)
    {
        await _permissionService.AuthorizeTeamActionAsync(userId, teamId, TeamAction.Update, ct);

        var team = await _teamRepository.GetTeamByIdAsync(teamId, ct)
            ?? throw new NotFoundException("Team", teamId);

        team.Name = dto.Name;
        team.Description = dto.Description;

        await _teamRepository.UpdateTeamAsync(team, ct);
        await _auditService.LogAsync(userId, "UpdateTeam", "Team", team.Id, new { team.Name }, ct);

        var role = team.Members.FirstOrDefault(m => m.UserId == userId)?.Role;
        return new TeamResponseDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            CreatedById = team.CreatedById,
            CreatedByName = team.CreatedBy?.FullName ?? "Unknown",
            CreatedAt = team.CreatedAt,
            MemberCount = team.Members.Count,
            CurrentUserRole = role
        };
    }

    public async Task DeleteTeamAsync(Guid teamId, Guid userId, CancellationToken ct = default)
    {
        await _permissionService.AuthorizeTeamActionAsync(userId, teamId, TeamAction.Delete, ct);

        var hasTasks = await _context.Tasks.AnyAsync(t => t.TeamId == teamId, ct);
        if (hasTasks)
        {
            throw new BusinessValidationException("Cannot delete a workspace that still has tasks.");
        }

        var hasProjects = await _context.Projects.AnyAsync(p => p.WorkspaceId == teamId, ct);
        if (hasProjects)
        {
            throw new BusinessValidationException("Cannot delete a workspace that still has projects.");
        }



        await _teamRepository.DeleteTeamAsync(teamId, ct);
        await _auditService.LogAsync(userId, "DeleteTeam", "Team", teamId, null, ct);
    }

    public async Task<IEnumerable<TeamMemberResponseDto>> GetTeamMembersAsync(Guid teamId, Guid userId, CancellationToken ct = default)
    {
        await _permissionService.AuthorizeTeamActionAsync(userId, teamId, TeamAction.View, ct);

        var members = await _teamRepository.GetTeamMembersAsync(teamId, ct);
        return members.Select(m => new TeamMemberResponseDto
        {
            Id = m.Id,
            UserId = m.UserId,
            FullName = m.User.FullName,
            Email = m.User.Email,
            AvatarUrl = m.User.AvatarUrl,
            Role = m.Role,
            JoinedAt = m.JoinedAt
        });
    }

    public async Task<TeamMemberResponseDto> AddTeamMemberAsync(Guid teamId, AddTeamMemberDto dto, Guid userId, CancellationToken ct = default)
    {
        await _permissionService.AuthorizeTeamActionAsync(userId, teamId, TeamAction.ManageMembers, ct);

        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct)
            ?? throw new NotFoundException("User not found with email: " + dto.Email);

        var existingMember = await _teamRepository.GetTeamMemberAsync(teamId, targetUser.Id, ct);
        if (existingMember != null)
            throw new ConflictException("User is already a member of this team.");

        var member = new TeamMember
        {
            TeamId = teamId,
            UserId = targetUser.Id,
            Role = dto.Role,
            JoinedAt = DateTime.UtcNow
        };

        await _teamRepository.AddTeamMemberAsync(member, ct);
        await _auditService.LogAsync(userId, "AddTeamMember", "Team", teamId, new { targetUserId = targetUser.Id, role = dto.Role.ToString() }, ct);

        return new TeamMemberResponseDto
        {
            Id = member.Id,
            UserId = targetUser.Id,
            FullName = targetUser.FullName,
            Email = targetUser.Email,
            AvatarUrl = targetUser.AvatarUrl,
            Role = member.Role,
            JoinedAt = member.JoinedAt
        };
    }

    public async Task RemoveTeamMemberAsync(Guid teamId, Guid targetUserId, Guid currentUserId, CancellationToken ct = default)
    {
        // Users can always remove themselves, otherwise they need ManageMembers permission
        if (currentUserId != targetUserId)
        {
            await _permissionService.AuthorizeTeamActionAsync(currentUserId, teamId, TeamAction.ManageMembers, ct);
        }

        var member = await _teamRepository.GetTeamMemberAsync(teamId, targetUserId, ct)
            ?? throw new NotFoundException("Team member not found.");

        if (member.Role == TeamRole.Owner)
        {
            var allMembers = await _teamRepository.GetTeamMembersAsync(teamId, ct);
            var ownerCount = allMembers.Count(m => m.Role == TeamRole.Owner);

            if (ownerCount <= 1)
                throw new BusinessValidationException("Cannot remove the last owner of the team.");

            if (currentUserId != targetUserId)
                await _permissionService.AuthorizeTeamActionAsync(currentUserId, teamId, TeamAction.ManageMembers, ct);
        }

        await _teamRepository.RemoveTeamMemberAsync(member, ct);
        await _auditService.LogAsync(currentUserId, "RemoveTeamMember", "Team", teamId, new { targetUserId }, ct);
    }

    public async Task<TeamMemberResponseDto> ChangeMemberRoleAsync(Guid teamId, Guid targetUserId, ChangeTeamRoleDto dto, Guid currentUserId, CancellationToken ct = default)
    {
        await _permissionService.AuthorizeTeamActionAsync(currentUserId, teamId, TeamAction.ManageMembers, ct);

        var member = await _teamRepository.GetTeamMemberAsync(teamId, targetUserId, ct)
            ?? throw new NotFoundException("Team member not found.");

        // Additional protection for Owners
        if (member.Role == TeamRole.Owner && dto.Role != TeamRole.Owner)
        {
            var allMembers = await _teamRepository.GetTeamMembersAsync(teamId, ct);
            var ownerCount = allMembers.Count(m => m.Role == TeamRole.Owner);

            if (ownerCount <= 1)
                throw new BusinessValidationException("Cannot change the role of the last owner.");
        }

        member.Role = dto.Role;
        await _teamRepository.UpdateTeamMemberAsync(member, ct);
        await _auditService.LogAsync(currentUserId, "ChangeMemberRole", "Team", teamId, new { targetUserId, newRole = dto.Role.ToString() }, ct);

        // Reload to get user data
        var updatedMember = await _context.TeamMembers
            .Include(m => m.User)
            .FirstAsync(m => m.Id == member.Id, ct);

        return new TeamMemberResponseDto
        {
            Id = updatedMember.Id,
            UserId = updatedMember.UserId,
            FullName = updatedMember.User.FullName,
            Email = updatedMember.User.Email,
            AvatarUrl = updatedMember.User.AvatarUrl,
            Role = updatedMember.Role,
            JoinedAt = updatedMember.JoinedAt
        };
    }
}





