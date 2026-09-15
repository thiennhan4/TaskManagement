using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using Xunit;

namespace TaskHub.Tests.Services;

public class ProjectBusinessRulesTests
{
    [Fact]
    public async Task CreateProjectAsync_PersonalProject_NormalizesWorkspaceAndUsesAuthenticatedOwner()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedTeam(context, workspaceId, ownerId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Personal",
            Slug = "personal",
            ProjectType = ProjectType.Personal,
            WorkspaceId = workspaceId,
            Visibility = ProjectVisibility.Public
        }, ownerId);

        result.OwnerId.Should().Be(ownerId);
        result.WorkspaceId.Should().BeNull();
        result.ProjectType.Should().Be(ProjectType.Personal);
        result.Visibility.Should().Be(ProjectVisibility.Private);
        context.ProjectMembers.Where(pm => pm.ProjectId == result.Id).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProjectAsync_TeamProjectRequiresWorkspace()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        SeedUser(context, userId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Team",
            Slug = "team",
            ProjectType = ProjectType.Team
        }, userId);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CreateProjectAsync_DeniesUnauthorizedWorkspace()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedUser(context, outsiderId);
        SeedTeam(context, workspaceId, ownerId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Team",
            Slug = "team",
            ProjectType = ProjectType.Team,
            WorkspaceId = workspaceId
        }, outsiderId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateProjectAsync_AuthorizedUserCreatesTeamProjectWithOwnerMember()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedTeam(context, workspaceId, ownerId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Team",
            Slug = "team",
            ProjectType = ProjectType.Team,
            WorkspaceId = workspaceId
        }, ownerId);

        result.ProjectType.Should().Be(ProjectType.Team);
        result.WorkspaceId.Should().Be(workspaceId);
        var member = await context.ProjectMembers.SingleAsync(pm => pm.ProjectId == result.Id && pm.UserId == ownerId);
        member.Role.Should().Be(ProjectRole.Owner);
    }

    [Fact]
    public async Task InviteMemberAsync_DeniesPersonalProjectInvites()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        SeedUser(context, ownerId);
        var project = SeedProject(context, ownerId, ProjectType.Personal);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.InviteMemberAsync(project.Id, new InviteMemberDto
        {
            Email = "invitee@example.com",
            Role = ProjectRole.Member
        }, ownerId);

        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task AcceptInvitationAsync_DeniesPersonalProjectMemberAddition()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedUser(context, inviteeId, "invitee@example.com");
        var project = SeedProject(context, ownerId, ProjectType.Personal);
        var invitation = new ProjectInvitation
        {
            ProjectId = project.Id,
            Project = project,
            InvitedByUserId = ownerId,
            InviteeEmail = "invitee@example.com",
            Role = ProjectRole.Member
        };
        context.ProjectInvitations.Add(invitation);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.AcceptInvitationAsync(invitation.Token, inviteeId);

        await act.Should().ThrowAsync<BusinessValidationException>();
    }

    [Fact]
    public async Task InviteMemberAsync_AllowsTeamProjectInvitation()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        SeedUser(context, ownerId);
        SeedUser(context, inviteeId, "invitee@example.com");
        SeedTeam(context, workspaceId, ownerId);
        var project = SeedProject(context, ownerId, ProjectType.Team, workspaceId);
        context.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = ownerId, Role = ProjectRole.Owner });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await service.InviteMemberAsync(project.Id, new InviteMemberDto
        {
            Email = "invitee@example.com",
            Role = ProjectRole.Member
        }, ownerId);

        var invitation = await context.ProjectInvitations.SingleAsync(i => i.ProjectId == project.Id);
        invitation.InviteeEmail.Should().Be("invitee@example.com");
        invitation.Role.Should().Be(ProjectRole.Member);
    }

    [Fact]
    public async Task CreateProjectAsync_CreatesDefaultBoardAndListsOnce()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        SeedUser(context, ownerId);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateProjectAsync(new CreateProjectDto
        {
            Name = "Personal",
            Slug = "personal",
            ProjectType = ProjectType.Personal
        }, ownerId);

        var repository = new ProjectRepository(context);
        var project = await repository.GetByIdAsync(result.Id);
        await repository.EnsureDefaultBoardStructureAsync(project!);

        var board = await context.Boards.SingleAsync(b => b.ProjectId == result.Id && b.Name == "Main Board");
        var listNames = await context.Lists
            .Where(l => l.BoardId == board.Id)
            .OrderBy(l => l.Position)
            .Select(l => l.Name)
            .ToListAsync();

        listNames.Should().Equal("To Do", "In Progress", "Done");
    }

    private static ProjectService CreateService(AppDbContext context)
    {
        var notificationService = Substitute.For<INotificationService>();
        notificationService
            .CreateNotificationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.CompletedTask);

        var emailService = Substitute.For<IEmailService>();
        emailService
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(Task.CompletedTask);

        return new ProjectService(
            new ProjectRepository(context),
            new TeamRepository(context),
            new UserRepository(context),
            new PermissionService(new ProjectRepository(context), new TeamRepository(context), new UserRepository(context)),
            Substitute.For<IAuditService>(),
            emailService,
            notificationService);
    }

    private static Project SeedProject(AppDbContext context, Guid ownerId, ProjectType type, Guid? workspaceId = null)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = $"Project {Guid.NewGuid():N}",
            Slug = $"project-{Guid.NewGuid():N}",
            OwnerId = ownerId,
            ProjectType = type,
            WorkspaceId = workspaceId,
            Visibility = type == ProjectType.Personal ? ProjectVisibility.Private : ProjectVisibility.TeamOnly
        };
        context.Projects.Add(project);
        return project;
    }

    private static void SeedTeam(AppDbContext context, Guid teamId, Guid ownerId)
    {
        context.Teams.Add(new Team { Id = teamId, Name = "Workspace", CreatedById = ownerId });
        context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = ownerId, Role = TeamRole.Owner });
    }

    private static void SeedUser(AppDbContext context, Guid userId, string? email = null)
    {
        context.Users.Add(new AppUser
        {
            Id = userId,
            Email = email ?? $"{userId:N}@example.com",
            FullName = "Test User",
            PasswordHash = "hash"
        });
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
