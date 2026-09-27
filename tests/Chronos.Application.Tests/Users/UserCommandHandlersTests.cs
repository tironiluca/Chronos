using Chronos.Application.Common;
using Chronos.Application.Users;
using Chronos.Application.Users.Commands.Login;
using Chronos.Application.Users.Commands.RegisterUser;
using Chronos.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Users;

public class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithNewEmail_HashesPasswordAndPersistsUser()
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByEmailAsync("luca@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Hash("SuperSecret1").Returns("hashed-value");
        var handler = new RegisterUserCommandHandler(userRepository, passwordHasher);

        var result = await handler.Handle(
            new RegisterUserCommand(Guid.NewGuid(), "luca@example.com", "Luca", "SuperSecret1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "luca@example.com" && u.PasswordHash == "hashed-value" && u.Role == UserRole.Employee),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAlreadyRegisteredEmail_ReturnsFailureWithoutHashingPassword()
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByEmailAsync("luca@example.com", Arg.Any<CancellationToken>())
            .Returns(new User(Guid.NewGuid(), "luca@example.com", "Luca", "existing-hash"));
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var handler = new RegisterUserCommandHandler(userRepository, passwordHasher);

        var result = await handler.Handle(
            new RegisterUserCommand(Guid.NewGuid(), "luca@example.com", "Luca", "SuperSecret1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
        await userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithCorrectCredentials_ReturnsTokenFromGenerator()
    {
        var user = new User(Guid.NewGuid(), "luca@example.com", "Luca", "stored-hash", UserRole.Approver);
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByEmailAsync("luca@example.com", Arg.Any<CancellationToken>()).Returns(user);
        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Verify("stored-hash", "correct-password").Returns(true);
        var tokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var expiresAt = DateTime.UtcNow.AddHours(1);
        tokenGenerator.Generate(user).Returns(new AuthTokenDto("jwt-token", expiresAt));
        var handler = new LoginCommandHandler(userRepository, passwordHasher, tokenGenerator);

        var result = await handler.Handle(new LoginCommand("luca@example.com", "correct-password"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("jwt-token");
        result.Value.Role.Should().Be("Approver");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsFailureWithoutGeneratingToken()
    {
        var user = new User(Guid.NewGuid(), "luca@example.com", "Luca", "stored-hash");
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByEmailAsync("luca@example.com", Arg.Any<CancellationToken>()).Returns(user);
        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Verify("stored-hash", "wrong-password").Returns(false);
        var tokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var handler = new LoginCommandHandler(userRepository, passwordHasher, tokenGenerator);

        var result = await handler.Handle(new LoginCommand("luca@example.com", "wrong-password"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        tokenGenerator.DidNotReceive().Generate(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ReturnsSameFailureAsWrongPassword()
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByEmailAsync("ghost@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var tokenGenerator = Substitute.For<IJwtTokenGenerator>();
        var handler = new LoginCommandHandler(userRepository, passwordHasher, tokenGenerator);

        var result = await handler.Handle(new LoginCommand("ghost@example.com", "whatever"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password.");
    }
}
