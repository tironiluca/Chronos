using Chronos.Application.Kanban;
using Chronos.Application.Kanban.Commands.AssignKanbanCardUser;
using Chronos.Application.Users;
using Chronos.Domain.Kanban;
using Chronos.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Kanban;

public class AssignKanbanCardUserCommandHandlerTests
{
    private static (Board board, KanbanColumn column, KanbanCard card) CreateBoardWithCard(Guid organizationId)
    {
        var board = new Board(organizationId, "Line 3 Kanban");
        var column = board.AddColumn("To Do", 0);
        var card = column.AddCard("Install PLC");
        return (board, column, card);
    }

    [Fact]
    public async Task Handle_WithUserInSameOrganization_AssignsUser()
    {
        var organizationId = Guid.NewGuid();
        var (board, column, card) = CreateBoardWithCard(organizationId);
        var user = new User(organizationId, "assignee@example.com", "Assignee", "hash", UserRole.Employee);
        var boardRepository = Substitute.For<IBoardRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        boardRepository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new AssignKanbanCardUserCommandHandler(boardRepository, userRepository);

        var result = await handler.Handle(
            new AssignKanbanCardUserCommand(organizationId, board.Id, column.Id, card.Id, user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        card.AssignedUserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_WithNullUserId_UnassignsCard()
    {
        var organizationId = Guid.NewGuid();
        var (board, column, card) = CreateBoardWithCard(organizationId);
        card.AssignUser(Guid.NewGuid());
        var boardRepository = Substitute.For<IBoardRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        boardRepository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        var handler = new AssignKanbanCardUserCommandHandler(boardRepository, userRepository);

        var result = await handler.Handle(
            new AssignKanbanCardUserCommand(organizationId, board.Id, column.Id, card.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        card.AssignedUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithUserInAnotherOrganization_ReturnsFailureWithoutAssigning()
    {
        var organizationId = Guid.NewGuid();
        var (board, column, card) = CreateBoardWithCard(organizationId);
        var outsider = new User(Guid.NewGuid(), "outsider@example.com", "Outsider", "hash", UserRole.Employee);
        var boardRepository = Substitute.For<IBoardRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        boardRepository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        userRepository.GetByIdAsync(outsider.Id, Arg.Any<CancellationToken>()).Returns(outsider);
        var handler = new AssignKanbanCardUserCommandHandler(boardRepository, userRepository);

        var result = await handler.Handle(
            new AssignKanbanCardUserCommand(organizationId, board.Id, column.Id, card.Id, outsider.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        card.AssignedUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithUnknownCard_ReturnsFailure()
    {
        var organizationId = Guid.NewGuid();
        var (board, column, _) = CreateBoardWithCard(organizationId);
        var boardRepository = Substitute.For<IBoardRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        boardRepository.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);
        var handler = new AssignKanbanCardUserCommandHandler(boardRepository, userRepository);

        var result = await handler.Handle(
            new AssignKanbanCardUserCommand(organizationId, board.Id, column.Id, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
