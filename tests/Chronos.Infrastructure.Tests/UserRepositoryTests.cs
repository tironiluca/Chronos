using Chronos.Domain.Users;
using Chronos.Infrastructure.Persistence;
using Chronos.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronos.Infrastructure.Tests;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ChronosDbContext _context;

    public UserRepositoryTests()
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
    public async Task GetByEmailAsync_IsCaseInsensitive()
    {
        var repository = new UserRepository(_context);
        await repository.AddAsync(new User(Guid.NewGuid(), "Luca@Example.com", "Luca", "hash"));
        await repository.SaveChangesAsync();

        var found = await repository.GetByEmailAsync("luca@example.com");

        found.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WhenNoMatch_ReturnsNull()
    {
        var repository = new UserRepository(_context);

        var found = await repository.GetByEmailAsync("ghost@example.com");

        found.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
