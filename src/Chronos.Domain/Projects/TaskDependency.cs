namespace Chronos.Domain.Projects;

// Link between two Gantt tasks (predecessor -> owning task). Persisted as an owned child.
public class TaskDependency
{
    public Guid PredecessorTaskId { get; private set; }
    public DependencyType Type { get; private set; }
    public int LagDays { get; private set; }

    private TaskDependency() { } // EF Core

    public TaskDependency(Guid predecessorTaskId, DependencyType type, int lagDays = 0)
    {
        PredecessorTaskId = predecessorTaskId;
        Type = type;
        LagDays = lagDays;
    }
}
