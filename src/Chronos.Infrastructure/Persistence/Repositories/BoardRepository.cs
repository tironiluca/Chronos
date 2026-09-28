using Chronos.Application.Kanban;
using Chronos.Domain.Kanban;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly ChronosDbContext _context;

    public BoardRepository(ChronosDbContext context)
    {
        _context = context;
    }

    public Task<Board?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Boards
            .Include(b => b.Columns)
            .ThenInclude(c => c.Cards)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Board>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await _context.Boards
            .Where(b => b.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Board>> GetDetailedByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await _context.Boards
            .Include(b => b.Columns)
            .ThenInclude(c => c.Cards)
            .Where(b => b.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Board board, CancellationToken cancellationToken = default) =>
        await _context.Boards.AddAsync(board, cancellationToken);

    // See ProjectRepository.TrackNewTask for the full explanation -- KanbanColumn.Id is already a
    // non-default Guid by the time EF's change tracker discovers it via an already-tracked Board's
    // owned collection, so EF's heuristic marks it Modified (0-row UPDATE) instead of Added unless
    // told otherwise explicitly.
    public void TrackNewColumn(KanbanColumn column) => _context.Entry(column).State = EntityState.Added;

    // Same reasoning as TrackNewColumn, one level deeper: for a card added to an already-persisted
    // column.
    public void TrackNewCard(KanbanCard card) => _context.Entry(card).State = EntityState.Added;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
