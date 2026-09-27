using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Queries.GetBoardById;

public class GetBoardByIdQueryHandler : IRequestHandler<GetBoardByIdQuery, Result<BoardDetailDto>>
{
    private readonly IBoardRepository _boardRepository;

    public GetBoardByIdQueryHandler(IBoardRepository boardRepository)
    {
        _boardRepository = boardRepository;
    }

    public async Task<Result<BoardDetailDto>> Handle(GetBoardByIdQuery request, CancellationToken cancellationToken)
    {
        var board = await _boardRepository.GetByIdAsync(request.BoardId, cancellationToken);

        if (board is null || board.OrganizationId != request.CallerOrganizationId)
            return Result.Failure<BoardDetailDto>($"Board '{request.BoardId}' was not found.");

        var columns = board.Columns
            .OrderBy(c => c.Order)
            .Select(c => new KanbanColumnDto(
                c.Id,
                c.BoardId,
                c.Name,
                c.Order,
                c.Cards.Select(card => new KanbanCardDto(card.Id, card.ColumnId, card.Title, card.AssignedUserId, card.GanttTaskId)).ToList()))
            .ToList();

        var dto = new BoardDetailDto(board.Id, board.OrganizationId, board.Name, board.ProjectId, columns);

        return Result.Success(dto);
    }
}
