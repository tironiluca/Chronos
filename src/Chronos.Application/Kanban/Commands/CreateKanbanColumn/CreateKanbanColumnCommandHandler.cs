using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Commands.CreateKanbanColumn;

public class CreateKanbanColumnCommandHandler : IRequestHandler<CreateKanbanColumnCommand, Result<Guid>>
{
    private readonly IBoardRepository _boardRepository;

    public CreateKanbanColumnCommandHandler(IBoardRepository boardRepository)
    {
        _boardRepository = boardRepository;
    }

    public async Task<Result<Guid>> Handle(CreateKanbanColumnCommand request, CancellationToken cancellationToken)
    {
        var board = await _boardRepository.GetByIdAsync(request.BoardId, cancellationToken);

        if (board is null || board.OrganizationId != request.CallerOrganizationId)
            return Result.Failure<Guid>($"Board '{request.BoardId}' was not found.");

        var column = board.AddColumn(request.Name, request.Order);
        _boardRepository.TrackNewColumn(column);
        await _boardRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(column.Id);
    }
}
