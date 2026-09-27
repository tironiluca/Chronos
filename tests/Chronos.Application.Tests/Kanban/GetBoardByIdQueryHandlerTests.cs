using Chronos.Application.Kanban;
using Chronos.Application.Kanban.Queries.GetBoardById;
using Chronos.Domain.Kanban;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Kanban;

public class GetBoardByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithBoardInCallerOrganization_ReturnsColumnsAndCardsMappedToDtos()
    {
        var organizationId = Guid.NewGuid();
        var board = new Board(organizationId, "Line 3 Kanban");
        var column = board.AddColumn("To Do", 0);
        var card = column.AddCard("Install PLC");
        var repository = Substitute.For<IBoardRepository>();
        repository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        var handler = new GetBoardByIdQueryHandler(repository);

        var result = await handler.Handle(new GetBoardByIdQuery(organizationId, board.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Columns.Should().ContainSingle(c => c.Id == column.Id)
            .Which.Cards.Should().ContainSingle(c => c.Id == card.Id && c.Title == "Install PLC");
    }

    [Fact]
    public async Task Handle_WithBoardInAnotherOrganization_ReturnsFailure()
    {
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");
        var repository = Substitute.For<IBoardRepository>();
        repository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        var handler = new GetBoardByIdQueryHandler(repository);

        var result = await handler.Handle(new GetBoardByIdQuery(Guid.NewGuid(), board.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
