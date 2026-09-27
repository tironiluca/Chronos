using Chronos.Application.Common;
using Chronos.Application.Users;
using MediatR;

namespace Chronos.Application.Projects.Commands.AssignGanttTaskUser;

public class AssignGanttTaskUserCommandHandler : IRequestHandler<AssignGanttTaskUserCommand, Result>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;

    public AssignGanttTaskUserCommandHandler(IProjectRepository projectRepository, IUserRepository userRepository)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(AssignGanttTaskUserCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null || project.OrganizationId != request.CallerOrganizationId)
            return Result.Failure($"Project '{request.ProjectId}' was not found.");

        var task = project.Tasks.FirstOrDefault(t => t.Id == request.TaskId);
        if (task is null)
            return Result.Failure($"Task '{request.TaskId}' was not found.");

        if (request.UserId is { } userId)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null || user.OrganizationId != request.CallerOrganizationId)
                return Result.Failure($"User '{userId}' was not found.");
        }

        task.AssignUser(request.UserId);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
