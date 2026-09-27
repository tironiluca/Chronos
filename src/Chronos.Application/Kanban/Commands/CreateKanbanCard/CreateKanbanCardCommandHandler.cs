using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Commands.CreateKanbanCard;

public class CreateKanbanCardCommandHandler : IRequestHandler<CreateKanbanCardCommand, Result<Guid>>
{
    private readonly IBoardRepository _boardRepository;

    public CreateKanbanCardCommandHandler(IBoardRepository boardRepository)
    {
        _boardRepository = boardRepository;
    }

    public async Task<Result<Guid>> Handle(CreateKanbanCardCommand request, CancellationToken cancellationToken)
    {
        var board = await _boardRepository.GetByIdAsync(request.BoardId, cancellationToken);

        if (board is null || board.OrganizationId != request.CallerOrganizationId)
            return Result.Failure<Guid>($"Board '{request.BoardId}' was not found.");

        var column = board.Columns.FirstOrDefault(c => c.Id == request.ColumnId);
        if (column is null)
            return Result.Failure<Guid>($"Column '{request.ColumnId}' was not found.");

        // GanttTaskId, like GanttTask.ParentTaskId, is a plain optional cross-reference -- not
        // validated for existence here, same treatment as that field.
        var card = column.AddCard(request.Title, request.GanttTaskId);
        _boardRepository.TrackNewCard(card);
        await _boardRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(card.Id);
    }
}
