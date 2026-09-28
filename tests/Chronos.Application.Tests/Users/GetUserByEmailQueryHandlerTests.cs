using Chronos.Application.Users;
using Chronos.Application.Users.Queries.GetUserByEmail;
using Chronos.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Users;

public class GetUserByEmailQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithUserInCallerOrganization_ReturnsMappedDto()
    {
        var organizationId = Guid.NewGuid();
        var user = new User(organizationId, "luca@example.com", "Luca Tironi", "hash", UserRole.Approver);
        var repository = Substitute.For<IUserRepository>();
        repository.GetByEmailAsync("luca@example.com", Arg.Any<CancellationToken>()).Returns(user);
        var handler = new GetUserByEmailQueryHandler(repository);

        var result = await handler.Handle(new GetUserByEmailQuery(organizationId, "luca@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new UserDto(user.Id, user.Email, "Luca Tironi", "Approver", null));
    }

    [Fact]
    public async Task Handle_WithUserInAnotherOrganization_ReturnsFailure()
    {
        var user = new User(Guid.NewGuid(), "luca@example.com", "Luca Tironi", "hash");
        var repository = Substitute.For<IUserRepository>();
        repository.GetByEmailAsync("luca@example.com", Arg.Any<CancellationToken>()).Returns(user);
        var handler = new GetUserByEmailQueryHandler(repository);

        var result = await handler.Handle(new GetUserByEmailQuery(Guid.NewGuid(), "luca@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNoMatchingUser_ReturnsFailure()
    {
        var repository = Substitute.For<IUserRepository>();
        repository.GetByEmailAsync("ghost@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new GetUserByEmailQueryHandler(repository);

        var result = await handler.Handle(new GetUserByEmailQuery(Guid.NewGuid(), "ghost@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
