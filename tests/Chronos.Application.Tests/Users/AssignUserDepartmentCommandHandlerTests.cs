using Chronos.Application.Organizations;
using Chronos.Application.Users;
using Chronos.Application.Users.Commands.AssignUserDepartment;
using Chronos.Domain.Organizations;
using Chronos.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Users;

public class AssignUserDepartmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithDepartmentInCallerOrganization_AssignsDepartment()
    {
        var organizationId = Guid.NewGuid();
        var user = new User(organizationId, "luca@example.com", "Luca", "hash");
        var department = new Department(organizationId, "Engineering", "ENG");

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var departmentRepository = Substitute.For<IDepartmentRepository>();
        departmentRepository.GetByIdAsync(department.Id, Arg.Any<CancellationToken>()).Returns(department);

        var handler = new AssignUserDepartmentCommandHandler(userRepository, departmentRepository);

        var result = await handler.Handle(
            new AssignUserDepartmentCommand(organizationId, user.Id, department.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.DepartmentId.Should().Be(department.Id);
        await userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullDepartmentId_UnassignsDepartment()
    {
        var organizationId = Guid.NewGuid();
        var user = new User(organizationId, "luca@example.com", "Luca", "hash");
        user.AssignDepartment(Guid.NewGuid());

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var departmentRepository = Substitute.For<IDepartmentRepository>();

        var handler = new AssignUserDepartmentCommandHandler(userRepository, departmentRepository);

        var result = await handler.Handle(
            new AssignUserDepartmentCommand(organizationId, user.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.DepartmentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithUserInAnotherOrganization_ReturnsFailure()
    {
        var user = new User(Guid.NewGuid(), "luca@example.com", "Luca", "hash");

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var departmentRepository = Substitute.For<IDepartmentRepository>();

        var handler = new AssignUserDepartmentCommandHandler(userRepository, departmentRepository);

        var result = await handler.Handle(
            new AssignUserDepartmentCommand(Guid.NewGuid(), user.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithDepartmentInAnotherOrganization_ReturnsFailureWithoutAssigning()
    {
        var organizationId = Guid.NewGuid();
        var user = new User(organizationId, "luca@example.com", "Luca", "hash");
        var department = new Department(Guid.NewGuid(), "Engineering", "ENG"); // different org

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var departmentRepository = Substitute.For<IDepartmentRepository>();
        departmentRepository.GetByIdAsync(department.Id, Arg.Any<CancellationToken>()).Returns(department);

        var handler = new AssignUserDepartmentCommandHandler(userRepository, departmentRepository);

        var result = await handler.Handle(
            new AssignUserDepartmentCommand(organizationId, user.Id, department.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        user.DepartmentId.Should().BeNull();
    }
}
