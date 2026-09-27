using Chronos.Application.Kanban;
using Chronos.Application.Kanban.Queries.GetBoardsByOrganization;
using Chronos.Domain.Kanban;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Kanban;

public class GetBoardsByOrganizationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsBoardsMappedToDtos()
    {
        var organizationId = Guid.NewGuid();
        var board = new Board(organizationId, "Line 3 Kanban");
        var repository = Substitute.For<IBoardRepository>();
        repository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>())
            .Returns(new[] { board });
        var handler = new GetBoardsByOrganizationQueryHandler(repository);

        var result = await handler.Handle(new GetBoardsByOrganizationQuery(organizationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(b => b.Id == board.Id && b.Name == "Line 3 Kanban");
    }
}
