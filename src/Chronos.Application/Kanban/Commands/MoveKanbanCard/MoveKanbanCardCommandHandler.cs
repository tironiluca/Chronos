using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Commands.MoveKanbanCard;

public class MoveKanbanCardCommandHandler : IRequestHandler<MoveKanbanCardCommand, Result>
{
    private readonly IBoardRepository _boardRepository;

    public MoveKanbanCardCommandHandler(IBoardRepository boardRepository)
    {
        _boardRepository = boardRepository;
    }

    public async Task<Result> Handle(MoveKanbanCardCommand request, CancellationToken cancellationToken)
    {
        var board = await _boardRepository.GetByIdAsync(request.BoardId, cancellationToken);

        if (board is null || board.OrganizationId != request.CallerOrganizationId)
            return Result.Failure($"Board '{request.BoardId}' was not found.");

        try
        {
            // A target column on another board is simply "not found" here: Board.MoveCard only
            // looks at its own columns, so cards can't leak across boards (or organizations).
            board.MoveCard(request.CardId, request.ColumnId, request.TargetColumnId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        // No TrackNew* call: the card is already tracked, EF just sees its owner FK change.
        // Covered by BoardRepositoryTests.MoveCard_OnReloadedBoard_ThenSaveChanges_PersistsNewColumnAndKeepsCardIdentity.
        await _boardRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
