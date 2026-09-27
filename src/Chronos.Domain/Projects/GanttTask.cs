using Chronos.Domain.Common;

namespace Chronos.Domain.Projects;

public class GanttTask : Entity
{
    private readonly List<TaskDependency> _dependencies = new();

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int ProgressPercent { get; private set; }
    public Guid? ParentTaskId { get; private set; } // hierarchical WBS support

    // Nullable: a task isn't required to have an assignee. Set via AssignUser, not the
    // constructor, so existing callers aren't forced to pass one -- see the resource-availability
    // epic in IMPLEMENTATION_PLAN.md, which cross-references this against a person's calendar.
    public Guid? AssignedUserId { get; private set; }

    public IReadOnlyCollection<TaskDependency> Dependencies => _dependencies.AsReadOnly();

    private GanttTask() { } // EF Core

    public GanttTask(Guid projectId, string name, DateOnly startDate, DateOnly endDate, Guid? parentTaskId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name is required.", nameof(name));
        if (endDate < startDate)
            throw new ArgumentException("End date cannot precede start date.", nameof(endDate));

        ProjectId = projectId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        ParentTaskId = parentTaskId;
        ProgressPercent = 0;
    }

    public void Reschedule(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot precede start date.", nameof(endDate));

        StartDate = startDate;
        EndDate = endDate;
    }

    public void UpdateProgress(int percent)
    {
        if (percent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Progress must be between 0 and 100.");

        ProgressPercent = percent;
    }

    // null unassigns the task.
    public void AssignUser(Guid? userId)
    {
        AssignedUserId = userId;
    }

    public void AddDependency(Guid predecessorTaskId, DependencyType type, int lagDays = 0)
    {
        if (predecessorTaskId == Id)
            throw new InvalidOperationException("A task cannot depend on itself.");

        _dependencies.Add(new TaskDependency(predecessorTaskId, type, lagDays));
    }
}
