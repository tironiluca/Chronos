using Chronos.Application.Kanban;
using Chronos.Application.Kanban.Commands.MoveKanbanCard;
using Chronos.Domain.Kanban;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Kanban;

public class MoveKanbanCardCommandHandlerTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly IBoardRepository _boardRepository = Substitute.For<IBoardRepository>();

    private Board CreateBoard(Guid? organizationId = null)
    {
        var board = new Board(organizationId ?? _organizationId, "Line 3 Kanban");
        _boardRepository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        return board;
    }

    [Fact]
    public async Task Handle_ToAnotherColumnOfTheBoard_MovesCardAndSaves()
    {
        var board = CreateBoard();
        var todo = board.AddColumn("To Do", 0);
        var done = board.AddColumn("Done", 1);
        var card = todo.AddCard("Install PLC");
        var handler = new MoveKanbanCardCommandHandler(_boardRepository);

        var result = await handler.Handle(new MoveKanbanCardCommand(_organizationId, board.Id, todo.Id, card.Id, done.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        done.Cards.Should().ContainSingle().Which.Should().BeSameAs(card);
        await _boardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ToColumnOfAnotherBoard_ReturnsFailureWithoutSaving()
    {
        var board = CreateBoard();
        var todo = board.AddColumn("To Do", 0);
        var card = todo.AddCard("Install PLC");
        var foreignColumn = CreateBoard().AddColumn("Done", 0);
        var handler = new MoveKanbanCardCommandHandler(_boardRepository);

        var result = await handler.Handle(new MoveKanbanCardCommand(_organizationId, board.Id, todo.Id, card.Id, foreignColumn.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        todo.Cards.Should().ContainSingle();
        await _boardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBoardInAnotherOrganization_ReturnsFailure()
    {
        var board = CreateBoard(Guid.NewGuid());
        var todo = board.AddColumn("To Do", 0);
        var done = board.AddColumn("Done", 1);
        var card = todo.AddCard("Install PLC");
        var handler = new MoveKanbanCardCommandHandler(_boardRepository);

        var result = await handler.Handle(new MoveKanbanCardCommand(_organizationId, board.Id, todo.Id, card.Id, done.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        todo.Cards.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithUnknownCard_ReturnsFailure()
    {
        var board = CreateBoard();
        var todo = board.AddColumn("To Do", 0);
        var done = board.AddColumn("Done", 1);
        var handler = new MoveKanbanCardCommandHandler(_boardRepository);

        var result = await handler.Handle(new MoveKanbanCardCommand(_organizationId, board.Id, todo.Id, Guid.NewGuid(), done.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
