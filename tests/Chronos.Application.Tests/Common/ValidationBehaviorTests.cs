using Chronos.Application.Common;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Xunit;

namespace Chronos.Application.Tests.Common;

public class ValidationBehaviorTests
{
    private record TestCommand(string Name) : IRequest<Result>;

    private record TestCommandWithValue(string Name) : IRequest<Result<Guid>>;

    private class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator() => RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
    }

    private class TestCommandWithValueValidator : AbstractValidator<TestCommandWithValue>
    {
        public TestCommandWithValueValidator() => RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
    }

    [Fact]
    public async Task Handle_WithFailingValidator_ReturnsFailureWithoutCallingNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(
            new TestCommand(""),
            () =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Name is required.");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithFailingValidator_ForGenericResult_ReturnsTypedFailureWithoutCallingNext()
    {
        var behavior = new ValidationBehavior<TestCommandWithValue, Result<Guid>>([new TestCommandWithValueValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(
            new TestCommandWithValue(""),
            () =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success(Guid.NewGuid()));
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Name is required.");
        result.Value.Should().Be(Guid.Empty);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithPassingValidator_CallsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandValidator()]);

        var result = await behavior.Handle(new TestCommand("valid"), () => Task.FromResult(Result.Success()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoValidatorsRegistered_CallsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([]);

        var result = await behavior.Handle(new TestCommand(""), () => Task.FromResult(Result.Success()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
