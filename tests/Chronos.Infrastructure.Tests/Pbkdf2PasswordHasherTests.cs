using Chronos.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace Chronos.Infrastructure.Tests;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_Succeeds()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        _hasher.Verify(hash, "correct-horse-battery-staple").Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_Fails()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        _hasher.Verify(hash, "wrong-password").Should().BeFalse();
    }

    [Fact]
    public void Hash_CalledTwiceWithSamePassword_ProducesDifferentOutput()
    {
        // Different random salt each time -- guards against a hasher that forgot to salt.
        var first = _hasher.Hash("same-password");
        var second = _hasher.Hash("same-password");

        first.Should().NotBe(second);
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalseInsteadOfThrowing()
    {
        _hasher.Verify("not-a-valid-hash", "whatever").Should().BeFalse();
    }
}
