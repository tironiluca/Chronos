using Chronos.Application.Common;
using Chronos.Application.Kanban;
using Chronos.Application.Leave;
using Chronos.Application.Organizations;
using Chronos.Application.Projects;
using Chronos.Application.Users;
using Chronos.Domain.Kanban;
using Chronos.Domain.Leave;
using Chronos.Domain.Organizations;
using Chronos.Domain.Projects;
using Chronos.Domain.Users;
using MediatR;

namespace Chronos.Application.Resources.Queries.GetResourceAvailability;

public class GetResourceAvailabilityQueryHandler
    : IRequestHandler<GetResourceAvailabilityQuery, Result<IReadOnlyList<ResourceAvailabilityDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public GetResourceAvailabilityQueryHandler(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IProjectRepository projectRepository,
        IBoardRepository boardRepository,
        ILeaveRequestRepository leaveRequestRepository)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _projectRepository = projectRepository;
        _boardRepository = boardRepository;
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<Result<IReadOnlyList<ResourceAvailabilityDto>>> Handle(
        GetResourceAvailabilityQuery request, CancellationToken cancellationToken)
    {
        HashSet<Guid>? departmentScope = null;
        if (request.DepartmentId is { } departmentId)
        {
            var departments = await _departmentRepository.GetByOrganizationAsync(request.CallerOrganizationId, cancellationToken);
            if (departments.All(d => d.Id != departmentId))
                return Result.Failure<IReadOnlyList<ResourceAvailabilityDto>>($"Department '{departmentId}' was not found.");

            departmentScope = ResolveWithDescendants(departmentId, departments);
        }

        var projects = await _projectRepository.GetDetailedByOrganizationAsync(request.CallerOrganizationId, cancellationToken);
        if (request.ProjectId is { } projectId)
        {
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            if (project is null)
                return Result.Failure<IReadOnlyList<ResourceAvailabilityDto>>($"Project '{projectId}' was not found.");

            projects = new[] { project };
        }

        var boards = await _boardRepository.GetDetailedByOrganizationAsync(request.CallerOrganizationId, cancellationToken);
        if (request.ProjectId is { } scopedProjectId)
            boards = boards.Where(b => b.ProjectId == scopedProjectId).ToList();

        var leaveRequests = await _leaveRequestRepository.GetByOrganizationAsync(request.CallerOrganizationId, cancellationToken);

        var users = await _userRepository.GetByOrganizationAsync(request.CallerOrganizationId, cancellationToken);
        if (departmentScope is not null)
            users = users.Where(u => u.DepartmentId is { } d && departmentScope.Contains(d)).ToList();

        var dtos = users
            .OrderBy(u => u.DisplayName)
            .Select(u => BuildAvailability(u, projects, boards, leaveRequests, request.From, request.To))
            .ToList();

        return Result.Success<IReadOnlyList<ResourceAvailabilityDto>>(dtos);
    }

    private static ResourceAvailabilityDto BuildAvailability(
        User user,
        IReadOnlyList<Project> projects,
        IReadOnlyList<Board> boards,
        IReadOnlyList<LeaveRequest> leaveRequests,
        DateOnly from,
        DateOnly to)
    {
        var approvedLeave = leaveRequests
            .Where(l => l.RequesterId == user.Id && l.Status == LeaveStatus.Approved && Overlaps(l.StartDate, l.EndDate, from, to))
            .Select(l => new LeavePeriodDto(l.Id, l.StartDate, l.EndDate, l.Type.ToString()))
            .ToList();

        var assignedTasks = projects
            .SelectMany(p => p.Tasks, (p, t) => (Project: p, Task: t))
            .Where(x => x.Task.AssignedUserId == user.Id && Overlaps(x.Task.StartDate, x.Task.EndDate, from, to))
            .Select(x => new AssignedTaskDto(x.Task.Id, x.Project.Id, x.Task.Name, x.Task.StartDate, x.Task.EndDate))
            .ToList();

        // KanbanCard has no schedule of its own, so it's included whenever assigned -- not
        // date-filtered like leave/tasks (see IMPLEMENTATION_PLAN.md's resource-availability epic).
        var assignedCards = boards
            .SelectMany(b => b.Columns, (b, c) => (Board: b, Column: c))
            .SelectMany(x => x.Column.Cards, (x, card) => (x.Board, x.Column, Card: card))
            .Where(x => x.Card.AssignedUserId == user.Id)
            .Select(x => new AssignedCardDto(x.Card.Id, x.Board.Id, x.Column.Id, x.Card.Title))
            .ToList();

        return new ResourceAvailabilityDto(user.Id, user.DisplayName, user.DepartmentId, approvedLeave, assignedTasks, assignedCards);
    }

    private static bool Overlaps(DateOnly start, DateOnly end, DateOnly from, DateOnly to) =>
        start <= to && end >= from;

    // Department counts per organization are small enough that walking the tree in memory (BFS
    // from the requested department down through ParentDepartmentId) is simpler than a recursive
    // SQL query -- see IDepartmentRepository.GetByOrganizationAsync's doc comment.
    private static HashSet<Guid> ResolveWithDescendants(Guid rootDepartmentId, IReadOnlyList<Department> departments)
    {
        var childrenByParent = departments
            .Where(d => d.ParentDepartmentId.HasValue)
            .GroupBy(d => d.ParentDepartmentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToList());

        var scope = new HashSet<Guid> { rootDepartmentId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootDepartmentId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!childrenByParent.TryGetValue(current, out var children))
                continue;

            foreach (var childId in children)
            {
                if (scope.Add(childId))
                    queue.Enqueue(childId);
            }
        }

        return scope;
    }
}
