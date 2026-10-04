using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using Xunit;

namespace TaskHub.Tests.Repositories;

public class ProjectBoardRepositoryTests
{
    [Fact]
    public async Task GetBoardsByProjectIdAsync_ReturnsBoardsForRequestedProjectOnly()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();
        var expectedBoard = new Board
        {
            Id = Guid.NewGuid(),
            Name = "Main Board",
            OwnerId = ownerId,
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var otherBoard = new Board
        {
            Id = Guid.NewGuid(),
            Name = "Other Board",
            OwnerId = ownerId,
            ProjectId = otherProjectId,
            CreatedAt = DateTime.UtcNow
        };

        context.Boards.AddRange(expectedBoard, otherBoard);
        await context.SaveChangesAsync();

        var repository = new BoardRepository(context);

        var boards = (await repository.GetBoardsByProjectIdAsync(projectId)).ToList();

        boards.Should().ContainSingle();
        boards[0].Id.Should().Be(expectedBoard.Id);
        boards.Should().NotContain(board => board.Id == otherBoard.Id);
    }

    [Fact]
    public async Task CanUserAccessProjectAsync_PreservesPersonalAndTeamMembershipAccess()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var personalProjectId = Guid.NewGuid();
        var teamProjectId = Guid.NewGuid();

        context.Projects.AddRange(
            new Project
            {
                Id = personalProjectId,
                Name = "Personal",
                Slug = "personal",
                OwnerId = ownerId,
                ProjectType = ProjectType.Personal
            },
            new Project
            {
                Id = teamProjectId,
                Name = "Team",
                Slug = "team",
                OwnerId = ownerId,
                ProjectType = ProjectType.Team,
                WorkspaceId = Guid.NewGuid()
            });
        context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = teamProjectId,
            UserId = memberId,
            Role = ProjectRole.Member
        });
        await context.SaveChangesAsync();

        var repository = new ProjectRepository(context);

        (await repository.CanUserAccessProjectAsync(personalProjectId, ownerId)).Should().BeTrue();
        (await repository.CanUserAccessProjectAsync(teamProjectId, memberId)).Should().BeTrue();
        (await repository.CanUserAccessProjectAsync(personalProjectId, outsiderId)).Should().BeFalse();
        (await repository.CanUserAccessProjectAsync(teamProjectId, outsiderId)).Should().BeFalse();
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
