using Chronos.Application.Kanban;
using Chronos.Application.Kanban.Commands.CreateBoard;
using Chronos.Application.Projects;
using Chronos.Domain.Kanban;
using Chronos.Domain.Projects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Kanban;

public class CreateBoardCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithNoProjectId_CreatesStandaloneBoard()
    {
        var organizationId = Guid.NewGuid();
        var boardRepository = Substitute.For<IBoardRepository>();
        var projectRepository = Substitute.For<IProjectRepository>();
        var handler = new CreateBoardCommandHandler(boardRepository, projectRepository);

        var result = await handler.Handle(new CreateBoardCommand(organizationId, "Line 3 Kanban", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await boardRepository.Received(1).AddAsync(Arg.Is<Board>(b => b.Id == result.Value), Arg.Any<CancellationToken>());
        await boardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProjectInCallerOrganization_CreatesBoardLinkedToProject()
    {
        var organizationId = Guid.NewGuid();
        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var boardRepository = Substitute.For<IBoardRepository>();
        var projectRepository = Substitute.For<IProjectRepository>();
        projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var handler = new CreateBoardCommandHandler(boardRepository, projectRepository);

        var result = await handler.Handle(new CreateBoardCommand(organizationId, "Line 3 Kanban", project.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await boardRepository.Received(1).AddAsync(Arg.Is<Board>(b => b.ProjectId == project.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProjectInAnotherOrganization_ReturnsFailureWithoutCreatingBoard()
    {
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var boardRepository = Substitute.For<IBoardRepository>();
        var projectRepository = Substitute.For<IProjectRepository>();
        projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var handler = new CreateBoardCommandHandler(boardRepository, projectRepository);

        var result = await handler.Handle(new CreateBoardCommand(Guid.NewGuid(), "Line 3 Kanban", project.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await boardRepository.DidNotReceive().AddAsync(Arg.Any<Board>(), Arg.Any<CancellationToken>());
    }
}
