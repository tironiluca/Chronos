using Chronos.Domain.Users;
using FluentAssertions;
using Xunit;

namespace Chronos.Domain.Tests.Users;

public class UserTests
{
    [Fact]
    public void Constructor_NormalizesEmailToLowercase()
    {
        var user = new User(Guid.NewGuid(), "Luca@Example.com", "Luca", "hash");

        user.Email.Should().Be("luca@example.com");
    }

    [Fact]
    public void Constructor_DefaultsRoleToEmployee()
    {
        var user = new User(Guid.NewGuid(), "luca@example.com", "Luca", "hash");

        user.Role.Should().Be(UserRole.Employee);
    }

    [Fact]
    public void Constructor_WithEmptyEmail_Throws()
    {
        var act = () => new User(Guid.NewGuid(), "", "Luca", "hash");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyPasswordHash_Throws()
    {
        var act = () => new User(Guid.NewGuid(), "luca@example.com", "Luca", "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChangeRole_UpdatesRole()
    {
        var user = new User(Guid.NewGuid(), "luca@example.com", "Luca", "hash");

        user.ChangeRole(UserRole.Approver);

        user.Role.Should().Be(UserRole.Approver);
    }
}
