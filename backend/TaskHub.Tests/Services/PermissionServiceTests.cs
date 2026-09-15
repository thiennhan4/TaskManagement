using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using Xunit;

namespace TaskHub.Tests.Services;

public class PermissionServiceTests
{
    [Fact]
    public async Task AuthorizeProjectActionAsync_AllowsPersonalProjectOwner()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var project = CreateProject(ownerId, ProjectType.Personal);
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.AuthorizeProjectActionAsync(ownerId, project, ProjectAction.View);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AuthorizeProjectActionAsync_DeniesPersonalProjectOutsider()
    {
        await using var context = CreateContext();
        var project = CreateProject(Guid.NewGuid(), ProjectType.Personal);
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.AuthorizeProjectActionAsync(Guid.NewGuid(), project, ProjectAction.View);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AuthorizeBoardActionAsync_DeniesPersonalProjectBoardOutsider()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var project = CreateProject(ownerId, ProjectType.Personal);
        var board = CreateBoard(ownerId, project.Id);
        context.Projects.Add(project);
        context.Boards.Add(board);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.AuthorizeBoardActionAsync(Guid.NewGuid(), board, BoardAction.View);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AuthorizeTaskActionAsync_DeniesPersonalProjectTaskOutsider()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var task = SeedProjectTask(context, ownerId, ProjectType.Personal);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.AuthorizeTaskActionAsync(outsiderId, task, TaskAction.View);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AuthorizeProjectActionAsync_AllowsTeamOwnerOperationalAccess()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var project = CreateProject(Guid.NewGuid(), ProjectType.Team, teamId);
        context.Projects.Add(project);
        context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = ownerId, Role = TeamRole.Owner });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.AuthorizeProjectActionAsync(ownerId, project, ProjectAction.Update);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AuthorizeProjectActionAsync_AllowsProjectAdminMemberManagement()
    {
        await using var context = CreateContext();
        var adminId = Guid.NewGuid();
        var project = CreateProject(Guid.NewGuid(), ProjectType.Team, Guid.NewGuid());
        context.Projects.Add(project);
        context.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = adminId, Role = ProjectRole.Admin });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.AuthorizeProjectActionAsync(adminId, project, ProjectAction.ManageMembers);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AuthorizeTaskActionAsync_AllowsProjectMemberNormalTaskWork()
    {
        await using var context = CreateContext();
        var memberId = Guid.NewGuid();
        var task = SeedProjectTask(context, Guid.NewGuid(), ProjectType.Team, memberId, ProjectRole.Member);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.AuthorizeTaskActionAsync(memberId, task, TaskAction.Update);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AuthorizeTaskActionAsync_DeniesProjectGuestWriteAccess()
    {
        await using var context = CreateContext();
        var guestId = Guid.NewGuid();
        var task = SeedProjectTask(context, Guid.NewGuid(), ProjectType.Team, guestId, ProjectRole.Guest);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var act = () => service.AuthorizeTaskActionAsync(guestId, task, TaskAction.Update);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    private static TaskItem SeedProjectTask(
        AppDbContext context,
        Guid ownerId,
        ProjectType projectType,
        Guid? memberId = null,
        ProjectRole? memberRole = null)
    {
        var project = CreateProject(ownerId, projectType, projectType == ProjectType.Team ? Guid.NewGuid() : null);
        var board = CreateBoard(ownerId, project.Id);
        var list = CreateList(board.Id);
        var task = CreateTask(ownerId, list.Id);

        board.Project = project;
        list.Board = board;
        task.List = list;

        context.Projects.Add(project);
        context.Boards.Add(board);
        context.Lists.Add(list);
        context.Tasks.Add(task);

        if (memberId.HasValue && memberRole.HasValue)
        {
            context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = memberId.Value,
                Role = memberRole.Value
            });
        }

        return task;
    }

    private static PermissionService CreateService(AppDbContext context)
    {
        return new PermissionService(
            new ProjectRepository(context),
            new TeamRepository(context),
            new UserRepository(context));
    }

    private static Project CreateProject(Guid ownerId, ProjectType projectType, Guid? workspaceId = null)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            Name = $"Project {Guid.NewGuid():N}",
            Slug = $"project-{Guid.NewGuid():N}",
            OwnerId = ownerId,
            ProjectType = projectType,
            WorkspaceId = workspaceId
        };
    }

    private static Board CreateBoard(Guid ownerId, Guid projectId)
    {
        return new Board
        {
            Id = Guid.NewGuid(),
            Name = "Board",
            OwnerId = ownerId,
            ProjectId = projectId
        };
    }

    private static BoardList CreateList(Guid boardId)
    {
        return new BoardList
        {
            Id = Guid.NewGuid(),
            Name = "To Do",
            BoardId = boardId,
            Position = 1
        };
    }

    private static TaskItem CreateTask(Guid ownerId, Guid listId, Guid? assignedToId = null)
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            OwnerId = ownerId,
            AssignedToId = assignedToId,
            ListId = listId
        };
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
