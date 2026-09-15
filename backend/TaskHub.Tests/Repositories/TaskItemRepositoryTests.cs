using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;
using TaskHub.Infrastructure.Repositories;
using Xunit;

namespace TaskHub.Tests.Repositories;

public class TaskItemRepositoryTests
{
    [Fact]
    public async Task GetByProjectIdAsync_ReturnsTasksBelongingToBoardsOfRequestedProjectOnly()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();
        var projectBoardId = Guid.NewGuid();
        var otherProjectBoardId = Guid.NewGuid();
        var projectListId = Guid.NewGuid();
        var otherProjectListId = Guid.NewGuid();
        var expectedTaskId = Guid.NewGuid();
        var otherProjectTaskId = Guid.NewGuid();
        var deletedTaskId = Guid.NewGuid();

        context.Boards.AddRange(
            new Board { Id = projectBoardId, Name = "Project Board", OwnerId = ownerId, ProjectId = projectId },
            new Board { Id = otherProjectBoardId, Name = "Other Project Board", OwnerId = ownerId, ProjectId = otherProjectId });
        context.Lists.AddRange(
            new BoardList { Id = projectListId, Name = "To Do", BoardId = projectBoardId, Position = 1 },
            new BoardList { Id = otherProjectListId, Name = "To Do", BoardId = otherProjectBoardId, Position = 1 });
        context.Tasks.AddRange(
            new TaskItem { Id = expectedTaskId, Title = "Project Task", ListId = projectListId, OwnerId = ownerId },
            new TaskItem { Id = otherProjectTaskId, Title = "Other Project Task", ListId = otherProjectListId, OwnerId = ownerId },
            new TaskItem { Id = deletedTaskId, Title = "Deleted Project Task", ListId = projectListId, OwnerId = ownerId, IsDeleted = true });
        await context.SaveChangesAsync();

        var repository = new TaskItemRepository(context);

        var tasks = (await repository.GetByProjectIdAsync(projectId)).ToList();

        tasks.Should().ContainSingle();
        tasks[0].Id.Should().Be(expectedTaskId);
        tasks.Should().NotContain(task => task.Id == otherProjectTaskId);
        tasks.Should().NotContain(task => task.Id == deletedTaskId);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
