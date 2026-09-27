using Chronos.Application.Projects;
using Chronos.Application.Projects.Commands.CreateProject;
using Chronos.Domain.Projects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Projects;

public class CreateProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_PersistsProjectAndReturnsItsId()
    {
        var repository = Substitute.For<IProjectRepository>();
        var handler = new CreateProjectCommandHandler(repository);
        var command = new CreateProjectCommand(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await repository.Received(1).AddAsync(Arg.Is<Project>(p => p.Name == "Line 3 Upgrade"), Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
