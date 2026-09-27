using Chronos.Application.Projects;
using Chronos.Application.Projects.Commands.AssignGanttTaskUser;
using Chronos.Application.Users;
using Chronos.Domain.Projects;
using Chronos.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Projects;

public class AssignGanttTaskUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithUserInCallerOrganization_AssignsTask()
    {
        var organizationId = Guid.NewGuid();
        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var assignee = new User(organizationId, "assignee@example.com", "Assignee", "hash");

        var projectRepository = Substitute.For<IProjectRepository>();
        projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(assignee.Id, Arg.Any<CancellationToken>()).Returns(assignee);

        var handler = new AssignGanttTaskUserCommandHandler(projectRepository, userRepository);

        var result = await handler.Handle(
            new AssignGanttTaskUserCommand(organizationId, project.Id, task.Id, assignee.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.AssignedUserId.Should().Be(assignee.Id);
        await projectRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullUserId_UnassignsTask()
    {
        var organizationId = Guid.NewGuid();
        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        task.AssignUser(Guid.NewGuid());

        var projectRepository = Substitute.For<IProjectRepository>();
        projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var userRepository = Substitute.For<IUserRepository>();

        var handler = new AssignGanttTaskUserCommandHandler(projectRepository, userRepository);

        var result = await handler.Handle(
            new AssignGanttTaskUserCommand(organizationId, project.Id, task.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.AssignedUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithProjectInAnotherOrganization_ReturnsFailure()
    {
        var project = new Project(Guid.NewGuid(), "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));

        var projectRepository = Substitute.For<IProjectRepository>();
        projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var userRepository = Substitute.For<IUserRepository>();

        var handler = new AssignGanttTaskUserCommandHandler(projectRepository, userRepository);

        var result = await handler.Handle(
            new AssignGanttTaskUserCommand(Guid.NewGuid(), project.Id, task.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithUnknownTask_ReturnsFailure()
    {
        var organizationId = Guid.NewGuid();
        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));

        var projectRepository = Substitute.For<IProjectRepository>();
        projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var userRepository = Substitute.For<IUserRepository>();

        var handler = new AssignGanttTaskUserCommandHandler(projectRepository, userRepository);

        var result = await handler.Handle(
            new AssignGanttTaskUserCommand(organizationId, project.Id, Guid.NewGuid(), null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithUserInAnotherOrganization_ReturnsFailureWithoutAssigning()
    {
        var organizationId = Guid.NewGuid();
        var project = new Project(organizationId, "Line 3 Upgrade", "L3U", new DateOnly(2026, 1, 1));
        var task = project.AddTask("Install PLC", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 10));
        var otherOrgUser = new User(Guid.NewGuid(), "assignee@example.com", "Assignee", "hash");

        var projectRepository = Substitute.For<IProjectRepository>();
        projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(otherOrgUser.Id, Arg.Any<CancellationToken>()).Returns(otherOrgUser);

        var handler = new AssignGanttTaskUserCommandHandler(projectRepository, userRepository);

        var result = await handler.Handle(
            new AssignGanttTaskUserCommand(organizationId, project.Id, task.Id, otherOrgUser.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        task.AssignedUserId.Should().BeNull();
    }
}
