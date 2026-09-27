using Chronos.Domain.Common;

namespace Chronos.Domain.Leave;

public class LeaveRequest : AggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public Guid RequesterId { get; private set; }
    public LeaveType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public LeaveStatus Status { get; private set; }
    public Guid? ApproverId { get; private set; }
    public string? RejectionReason { get; private set; }

    private LeaveRequest() { } // EF Core

    public LeaveRequest(Guid organizationId, Guid requesterId, LeaveType type, DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot precede start date.", nameof(endDate));

        OrganizationId = organizationId;
        RequesterId = requesterId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Status = LeaveStatus.Pending;
    }

    public void Approve(Guid approverId)
    {
        if (Status != LeaveStatus.Pending)
            throw new InvalidOperationException($"Only a Pending request can be approved. Current status: {Status}.");

        Status = LeaveStatus.Approved;
        ApproverId = approverId;
    }

    public void Reject(Guid approverId, string reason)
    {
        if (Status != LeaveStatus.Pending)
            throw new InvalidOperationException($"Only a Pending request can be rejected. Current status: {Status}.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A rejection reason is required.", nameof(reason));

        Status = LeaveStatus.Rejected;
        ApproverId = approverId;
        RejectionReason = reason;
    }
}
