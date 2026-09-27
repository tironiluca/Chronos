using Chronos.Application.Kanban;
using Chronos.Application.Kanban.Commands.CreateKanbanCard;
using Chronos.Domain.Kanban;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Kanban;

public class CreateKanbanCardCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithColumnOnBoardInCallerOrganization_AddsCardAndReturnsItsId()
    {
        var organizationId = Guid.NewGuid();
        var board = new Board(organizationId, "Line 3 Kanban");
        var column = board.AddColumn("To Do", 0);
        var repository = Substitute.For<IBoardRepository>();
        repository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        var handler = new CreateKanbanCardCommandHandler(repository);

        var result = await handler.Handle(
            new CreateKanbanCardCommand(organizationId, board.Id, column.Id, "Install PLC", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        column.Cards.Should().ContainSingle(c => c.Id == result.Value && c.Title == "Install PLC");
        repository.Received(1).TrackNewCard(Arg.Is<KanbanCard>(c => c.Id == result.Value));
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBoardInAnotherOrganization_ReturnsFailureWithoutAddingCard()
    {
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");
        var column = board.AddColumn("To Do", 0);
        var repository = Substitute.For<IBoardRepository>();
        repository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        var handler = new CreateKanbanCardCommandHandler(repository);

        var result = await handler.Handle(
            new CreateKanbanCardCommand(Guid.NewGuid(), board.Id, column.Id, "Install PLC", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        column.Cards.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithUnknownColumn_ReturnsFailure()
    {
        var organizationId = Guid.NewGuid();
        var board = new Board(organizationId, "Line 3 Kanban");
        var repository = Substitute.For<IBoardRepository>();
        repository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        var handler = new CreateKanbanCardCommandHandler(repository);

        var result = await handler.Handle(
            new CreateKanbanCardCommand(organizationId, board.Id, Guid.NewGuid(), "Install PLC", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
