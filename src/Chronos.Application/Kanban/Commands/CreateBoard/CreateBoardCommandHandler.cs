using Chronos.Application.Common;
using Chronos.Application.Projects;
using Chronos.Domain.Kanban;
using MediatR;

namespace Chronos.Application.Kanban.Commands.CreateBoard;

public class CreateBoardCommandHandler : IRequestHandler<CreateBoardCommand, Result<Guid>>
{
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;

    public CreateBoardCommandHandler(IBoardRepository boardRepository, IProjectRepository projectRepository)
    {
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
    }

    public async Task<Result<Guid>> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
    {
        if (request.ProjectId is { } projectId)
        {
            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

            // Same failure for "not found" and "belongs to another organization" -- mirrors
            // CreateDepartmentCommandHandler's ParentDepartmentId check: never let the response
            // reveal whether a project id exists in someone else's org.
            if (project is null || project.OrganizationId != request.CallerOrganizationId)
                return Result.Failure<Guid>($"Project '{projectId}' was not found.");
        }

        var board = new Board(request.CallerOrganizationId, request.Name, request.ProjectId);

        await _boardRepository.AddAsync(board, cancellationToken);
        await _boardRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(board.Id);
    }
}
