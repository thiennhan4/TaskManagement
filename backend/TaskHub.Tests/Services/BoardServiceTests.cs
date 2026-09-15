using FluentAssertions;
using NSubstitute;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services;
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
        projectRepository.CanUserAccessProjectAsync(projectId, userId).Returns(true);
        boardRepository.GetBoardsByProjectIdAsync(projectId).Returns(boards);
        var service = new BoardService(boardRepository, projectRepository);

        var result = (await service.GetProjectBoardsAsync(projectId, userId)).ToList();

        result.Should().BeEquivalentTo(boards);
    }

    [Fact]
    public async Task GetProjectBoardsAsync_WhenUserCannotAccessProject_ThrowsForbidden()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var boardRepository = Substitute.For<IBoardRepository>();
        var projectRepository = Substitute.For<IProjectRepository>();
        projectRepository.CanUserAccessProjectAsync(projectId, userId).Returns(false);
        var service = new BoardService(boardRepository, projectRepository);

        var act = () => service.GetProjectBoardsAsync(projectId, userId);

        await act.Should().ThrowAsync<ForbiddenException>();
        await boardRepository.DidNotReceive().GetBoardsByProjectIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
