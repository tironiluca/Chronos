using Chronos.Application.Organizations;
using Chronos.Application.Organizations.Commands.CreateDepartment;
using Chronos.Domain.Organizations;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Chronos.Application.Tests.Organizations;

public class CreateDepartmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithNewCode_PersistsDepartmentAndReturnsItsId()
    {
        var repository = Substitute.For<IDepartmentRepository>();
        repository.GetByCodeAsync(Arg.Any<Guid>(), "ENG", Arg.Any<CancellationToken>()).Returns((Department?)null);
        var handler = new CreateDepartmentCommandHandler(repository);
        var organizationId = Guid.NewGuid();

        var result = await handler.Handle(
            new CreateDepartmentCommand(organizationId, "Engineering", "eng", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await repository.Received(1).AddAsync(
            Arg.Is<Department>(d => d.Name == "Engineering" && d.Code == "ENG" && d.OrganizationId == organizationId),
            Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAlreadyRegisteredCode_ReturnsFailureWithoutPersisting()
    {
        var repository = Substitute.For<IDepartmentRepository>();
        var organizationId = Guid.NewGuid();
        repository.GetByCodeAsync(organizationId, "ENG", Arg.Any<CancellationToken>())
            .Returns(new Department(organizationId, "Engineering", "ENG"));
        var handler = new CreateDepartmentCommandHandler(repository);

        var result = await handler.Handle(
            new CreateDepartmentCommand(organizationId, "Engineering Redux", "ENG", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await repository.DidNotReceive().AddAsync(Arg.Any<Department>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithParentInAnotherOrganization_ReturnsFailureWithoutPersisting()
    {
        var repository = Substitute.For<IDepartmentRepository>();
        var organizationId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        repository.GetByCodeAsync(organizationId, "PLAT", Arg.Any<CancellationToken>()).Returns((Department?)null);
        repository.GetByIdAsync(parentId, Arg.Any<CancellationToken>())
            .Returns(new Department(Guid.NewGuid(), "Engineering", "ENG")); // different org

        var handler = new CreateDepartmentCommandHandler(repository);

        var result = await handler.Handle(
            new CreateDepartmentCommand(organizationId, "Platform", "plat", parentId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await repository.DidNotReceive().AddAsync(Arg.Any<Department>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithParentInSameOrganization_Succeeds()
    {
        var repository = Substitute.For<IDepartmentRepository>();
        var organizationId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        repository.GetByCodeAsync(organizationId, "PLAT", Arg.Any<CancellationToken>()).Returns((Department?)null);
        repository.GetByIdAsync(parentId, Arg.Any<CancellationToken>())
            .Returns(new Department(organizationId, "Engineering", "ENG"));

        var handler = new CreateDepartmentCommandHandler(repository);

        var result = await handler.Handle(
            new CreateDepartmentCommand(organizationId, "Platform", "plat", parentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).AddAsync(
            Arg.Is<Department>(d => d.ParentDepartmentId == parentId), Arg.Any<CancellationToken>());
    }
}
