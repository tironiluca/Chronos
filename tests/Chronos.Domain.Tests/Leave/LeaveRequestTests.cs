using Chronos.Domain.Leave;
using FluentAssertions;
using Xunit;

namespace Chronos.Domain.Tests.Leave;

public class LeaveRequestTests
{
    [Fact]
    public void Approve_WhenPending_SetsStatusToApproved()
    {
        var request = new LeaveRequest(Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));

        request.Approve(Guid.NewGuid());

        request.Status.Should().Be(LeaveStatus.Approved);
    }

    [Fact]
    public void Reject_WithoutReason_Throws()
    {
        var request = new LeaveRequest(Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));

        var act = () => request.Reject(Guid.NewGuid(), "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_Throws()
    {
        var request = new LeaveRequest(Guid.NewGuid(), Guid.NewGuid(), LeaveType.Vacation,
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 17));
        request.Approve(Guid.NewGuid());

        var act = () => request.Approve(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }
}
