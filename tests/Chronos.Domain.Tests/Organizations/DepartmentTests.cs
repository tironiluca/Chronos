using Chronos.Domain.Organizations;
using FluentAssertions;
using Xunit;

namespace Chronos.Domain.Tests.Organizations;

public class DepartmentTests
{
    [Fact]
    public void Constructor_UppercasesCode()
    {
        var department = new Department(Guid.NewGuid(), "Engineering", "eng");

        department.Code.Should().Be("ENG");
    }

    [Fact]
    public void Constructor_DefaultsParentDepartmentIdToNull()
    {
        var department = new Department(Guid.NewGuid(), "Engineering", "ENG");

        department.ParentDepartmentId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithParentDepartmentId_SetsIt()
    {
        var parentId = Guid.NewGuid();

        var department = new Department(Guid.NewGuid(), "Platform", "PLAT", parentId);

        department.ParentDepartmentId.Should().Be(parentId);
    }

    [Fact]
    public void Constructor_WithEmptyOrganizationId_Throws()
    {
        var act = () => new Department(Guid.Empty, "Engineering", "ENG");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new Department(Guid.NewGuid(), "", "ENG");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyCode_Throws()
    {
        var act = () => new Department(Guid.NewGuid(), "Engineering", "");

        act.Should().Throw<ArgumentException>();
    }
}
