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

    // Graph-level invariants for a dependency edge predecessor -> task: both tasks belong to this
    // project, and the edge doesn't close a cycle (a cycle makes the schedule unsatisfiable and
    // would loop any renderer/scheduler that walks the graph). Task-local checks (self-reference,
    // duplicate edge) are GanttTask.AddDependency's.
    public TaskDependency AddTaskDependency(Guid taskId, Guid predecessorTaskId, DependencyType type, int lagDays = 0)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new InvalidOperationException($"Task '{taskId}' was not found.");
        if (_tasks.All(t => t.Id != predecessorTaskId))
            throw new InvalidOperationException($"Predecessor task '{predecessorTaskId}' was not found.");
        if (predecessorTaskId != taskId && DependsOn(predecessorTaskId, taskId))
            throw new InvalidOperationException("Dependency would create a cycle.");

        return task.AddDependency(predecessorTaskId, type, lagDays);
    }

    // True if taskId transitively depends on ancestorTaskId (walks predecessor edges).
    private bool DependsOn(Guid taskId, Guid ancestorTaskId)
    {
        var byId = _tasks.ToDictionary(t => t.Id);
        var visited = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(taskId);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current) || !byId.TryGetValue(current, out var node))
                continue;

            foreach (var dependency in node.Dependencies)
            {
                if (dependency.PredecessorTaskId == ancestorTaskId)
                    return true;
                pending.Push(dependency.PredecessorTaskId);
            }
        }

        return false;
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
