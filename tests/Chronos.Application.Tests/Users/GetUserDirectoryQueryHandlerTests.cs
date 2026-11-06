using Chronos.Application.Users;
using Chronos.Application.Users.Queries.GetUserDirectory;
using Chronos.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Users;

public class GetUserDirectoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCallerOrganizationUsersOrderedByDisplayName()
    {
        var organizationId = Guid.NewGuid();
        var zoe = new User(organizationId, "zoe@example.com", "zoe", "hash");
        var anna = new User(organizationId, "anna@example.com", "Anna", "hash");
        var repository = Substitute.For<IUserRepository>();
        repository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new List<User> { zoe, anna });
        var handler = new GetUserDirectoryQueryHandler(repository);

        var result = await handler.Handle(new GetUserDirectoryQuery(organizationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(u => u.DisplayName).Should().Equal("Anna", "zoe");
        result.Value![0].Id.Should().Be(anna.Id);
    }

    [Fact]
    public async Task Handle_WithNoUsers_ReturnsEmptyList()
    {
        var organizationId = Guid.NewGuid();
        var repository = Substitute.For<IUserRepository>();
        repository.GetByOrganizationAsync(organizationId, Arg.Any<CancellationToken>()).Returns(new List<User>());
        var handler = new GetUserDirectoryQueryHandler(repository);

        var result = await handler.Handle(new GetUserDirectoryQuery(organizationId), CancellationToken.None);

        result.Value.Should().BeEmpty();
    }
}
