using Chronos.Domain.Kanban;

namespace Chronos.Application.Kanban;

public interface IBoardRepository
{
    Task<Board?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Board>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task AddAsync(Board board, CancellationToken cancellationToken = default);

    // Call after adding a column to an already-persisted Board's Columns collection -- see
    // IProjectRepository.TrackNewTask for why this is necessary.
    void TrackNewColumn(KanbanColumn column);

    // Call after adding a card to an already-persisted KanbanColumn's Cards collection -- same
    // reasoning as TrackNewColumn, one level deeper.
    void TrackNewCard(KanbanCard card);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
