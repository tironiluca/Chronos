using Chronos.Domain.Kanban;
using Chronos.Infrastructure.Persistence;
using Chronos.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronos.Infrastructure.Tests;

// Fast, provider-agnostic tests run against Sqlite in-memory on every build -- see
// ProjectRepositoryTests for the same treatment on the Project/GanttTask aggregate.
public class BoardRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ChronosDbContext _context;

    public BoardRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ChronosDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ChronosDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsBoardWithColumnsAndCards()
    {
        var repository = new BoardRepository(_context);
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");
        var column = board.AddColumn("To Do", 0);
        column.AddCard("Install PLC");

        await repository.AddAsync(board);
        await repository.SaveChangesAsync();

        var reloaded = await repository.GetByIdAsync(board.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Columns.Should().ContainSingle(c => c.Name == "To Do")
            .Which.Cards.Should().ContainSingle(c => c.Title == "Install PLC");
    }

    // Regression test: adding a column to a Board reloaded from the DB (rather than one just
    // constructed) hits the same EF owned-collection Added/Modified heuristic bug as
    // ProjectRepositoryTests.AddTask_ToReloadedProject_ThenSaveChanges_Persists -- see
    // BoardRepository.TrackNewColumn.
    [Fact]
    public async Task AddColumn_ToReloadedBoard_ThenSaveChanges_Persists()
    {
        var repository = new BoardRepository(_context);
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");
        await repository.AddAsync(board);
        await repository.SaveChangesAsync();

        var reloaded = await repository.GetByIdAsync(board.Id);
        var column = reloaded!.AddColumn("To Do", 0);
        repository.TrackNewColumn(column);

        await repository.SaveChangesAsync();

        var reloadedAgain = await repository.GetByIdAsync(board.Id);
        reloadedAgain!.Columns.Should().ContainSingle(c => c.Name == "To Do");
    }

    // Same class of bug, one level deeper: a card added to a column that's already part of an
    // already-persisted (reloaded) board -- see BoardRepository.TrackNewCard.
    [Fact]
    public async Task AddCard_ToColumnOfReloadedBoard_ThenSaveChanges_Persists()
    {
        var repository = new BoardRepository(_context);
        var board = new Board(Guid.NewGuid(), "Line 3 Kanban");
        board.AddColumn("To Do", 0);
        await repository.AddAsync(board);
        await repository.SaveChangesAsync();

        var reloaded = await repository.GetByIdAsync(board.Id);
        var column = reloaded!.Columns.Single();
        var card = column.AddCard("Install PLC");
        repository.TrackNewCard(card);

        await repository.SaveChangesAsync();

        var reloadedAgain = await repository.GetByIdAsync(board.Id);
        reloadedAgain!.Columns.Single().Cards.Should().ContainSingle(c => c.Title == "Install PLC");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
