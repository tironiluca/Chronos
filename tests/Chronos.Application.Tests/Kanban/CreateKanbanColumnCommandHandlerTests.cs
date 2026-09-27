using Chronos.Application.Kanban;
using Chronos.Application.Kanban.Commands.CreateKanbanColumn;
using Chronos.Domain.Kanban;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Kanban;

public class CreateKanbanColumnCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithBoardInCallerOrganization_AddsColumnAndReturnsItsId()
    {
        var organizationId = Guid.NewGuid();
        var board = new Board(organizationId, "Line 3 Kanban");
        var repository = Substitute.For<IBoardRepository>();
        repository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        var handler = new CreateKanbanColumnCommandHandler(repository);

        var result = await handler.Handle(new CreateKanbanColumnCommand(organizationId, board.Id, "To Do", 0), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        board.Columns.Should().ContainSingle(c => c.Id == result.Value && c.Name == "To Do");
        // TrackNewColumn must be called -- see BoardRepository for why (same EF owned-collection
        // Added/Modified heuristic as ProjectRepository.TrackNewTask).
        repository.Received(1).TrackNewColumn(Arg.Is<KanbanColumn>(c => c.Id == result.Value));
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBoardInAnotherOrganization_ReturnsFailureWithoutAddingColumn()
    {
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");
        var repository = Substitute.For<IBoardRepository>();
        repository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        var handler = new CreateKanbanColumnCommandHandler(repository);

        var result = await handler.Handle(new CreateKanbanColumnCommand(Guid.NewGuid(), board.Id, "To Do", 0), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        board.Columns.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithUnknownBoard_ReturnsFailure()
    {
        var repository = Substitute.For<IBoardRepository>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Board?)null);
        var handler = new CreateKanbanColumnCommandHandler(repository);

        var result = await handler.Handle(new CreateKanbanColumnCommand(Guid.NewGuid(), Guid.NewGuid(), "To Do", 0), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
