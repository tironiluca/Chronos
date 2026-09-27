using Chronos.Domain.Common;

namespace Chronos.Domain.Projects;

public class Project : AggregateRoot
{
    private readonly List<GanttTask> _tasks = new();

    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public ProjectStatus Status { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    public IReadOnlyCollection<GanttTask> Tasks => _tasks.AsReadOnly();

    private Project() { } // EF Core

    public Project(Guid organizationId, string name, string code, DateOnly startDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Project code is required.", nameof(code));

        OrganizationId = organizationId;
        Name = name;
        Code = code.ToUpperInvariant();
        StartDate = startDate;
        Status = ProjectStatus.Planned;
    }

    public GanttTask AddTask(string name, DateOnly startDate, DateOnly endDate, Guid? parentTaskId = null)
    {
        var task = new GanttTask(Id, name, startDate, endDate, parentTaskId);
        _tasks.Add(task);
        return task;
    }

    public void Activate()
    {
        if (Status != ProjectStatus.Planned)
            throw new InvalidOperationException($"Only a Planned project can be activated. Current status: {Status}.");

        Status = ProjectStatus.Active;
    }

    public void Complete(DateOnly endDate)
    {
        if (Status != ProjectStatus.Active)
            throw new InvalidOperationException($"Only an Active project can be completed. Current status: {Status}.");

        Status = ProjectStatus.Completed;
        EndDate = endDate;
    }
}
