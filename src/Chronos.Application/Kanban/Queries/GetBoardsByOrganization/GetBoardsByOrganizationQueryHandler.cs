using Chronos.Application.Common;
using MediatR;

namespace Chronos.Application.Kanban.Queries.GetBoardsByOrganization;

public class GetBoardsByOrganizationQueryHandler : IRequestHandler<GetBoardsByOrganizationQuery, Result<IReadOnlyList<BoardDto>>>
{
    private readonly IBoardRepository _boardRepository;

    public GetBoardsByOrganizationQueryHandler(IBoardRepository boardRepository)
    {
        _boardRepository = boardRepository;
    }

    public async Task<Result<IReadOnlyList<BoardDto>>> Handle(GetBoardsByOrganizationQuery request, CancellationToken cancellationToken)
    {
        var boards = await _boardRepository.GetByOrganizationAsync(request.CallerOrganizationId, cancellationToken);

        var dtos = boards
            .Select(b => new BoardDto(b.Id, b.OrganizationId, b.Name, b.ProjectId))
            .ToList();

        return Result.Success<IReadOnlyList<BoardDto>>(dtos);
    }
}
