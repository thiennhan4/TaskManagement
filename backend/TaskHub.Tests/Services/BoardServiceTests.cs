using FluentAssertions;
using NSubstitute;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using Xunit;

namespace TaskHub.Tests.Services;

public class BoardServiceTests
{
    [Fact]
    public async Task GetProjectBoardsAsync_WhenUserHasProjectAccess_ReturnsProjectBoards()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var boards = new[]
        {
            new Board { Id = Guid.NewGuid(), Name = "Main Board", OwnerId = userId, ProjectId = projectId }
        };
        var boardRepository = Substitute.For<IBoardRepository>();
        var projectRepository = Substitute.For<IProjectRepository>();
        var permissionService = Substitute.For<IPermissionService>();
        var project = new Project
        {
            Id = projectId,
            Name = "Project",
            Slug = "project",
            OwnerId = userId,
            ProjectType = ProjectType.Personal
        };

        projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        permissionService
            .AuthorizeProjectActionAsync(userId, project, ProjectAction.View, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        boardRepository.GetBoardsByProjectIdAsync(projectId).Returns(boards);
        var service = new BoardService(boardRepository, projectRepository, permissionService, Substitute.For<IBoardReadRepository>());

        var result = (await service.GetProjectBoardsAsync(projectId, userId)).ToList();

        result.Should().BeEquivalentTo(boards.Select(BoardMapping.Summary));
        await permissionService.Received(1)
            .AuthorizeProjectActionAsync(userId, project, ProjectAction.View, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProjectBoardsAsync_WhenUserCannotAccessProject_ThrowsForbidden()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var boardRepository = Substitute.For<IBoardRepository>();
        var projectRepository = Substitute.For<IProjectRepository>();
        var permissionService = Substitute.For<IPermissionService>();
        var project = new Project
        {
            Id = projectId,
            Name = "Project",
            Slug = "project",
            OwnerId = Guid.NewGuid(),
            ProjectType = ProjectType.Personal
        };

        projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        permissionService
            .AuthorizeProjectActionAsync(userId, project, ProjectAction.View, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ForbiddenException("Forbidden"));
        var service = new BoardService(boardRepository, projectRepository, permissionService, Substitute.For<IBoardReadRepository>());

        var act = () => service.GetProjectBoardsAsync(projectId, userId);

        await act.Should().ThrowAsync<ForbiddenException>();
        await boardRepository.DidNotReceive().GetBoardsByProjectIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
