using Chronos.Application.Common;
using Chronos.Application.Users;
using MediatR;

namespace Chronos.Application.Kanban.Commands.AssignKanbanCardUser;

public class AssignKanbanCardUserCommandHandler : IRequestHandler<AssignKanbanCardUserCommand, Result>
{
    private readonly IBoardRepository _boardRepository;
    private readonly IUserRepository _userRepository;

    public AssignKanbanCardUserCommandHandler(IBoardRepository boardRepository, IUserRepository userRepository)
    {
        _boardRepository = boardRepository;
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(AssignKanbanCardUserCommand request, CancellationToken cancellationToken)
    {
        var board = await _boardRepository.GetByIdAsync(request.BoardId, cancellationToken);

        if (board is null || board.OrganizationId != request.CallerOrganizationId)
            return Result.Failure($"Board '{request.BoardId}' was not found.");

        var column = board.Columns.FirstOrDefault(c => c.Id == request.ColumnId);
        if (column is null)
            return Result.Failure($"Column '{request.ColumnId}' was not found.");

        var card = column.Cards.FirstOrDefault(c => c.Id == request.CardId);
        if (card is null)
            return Result.Failure($"Card '{request.CardId}' was not found.");

        if (request.UserId is { } userId)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null || user.OrganizationId != request.CallerOrganizationId)
                return Result.Failure($"User '{userId}' was not found.");
        }

        card.AssignUser(request.UserId);
        await _boardRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
