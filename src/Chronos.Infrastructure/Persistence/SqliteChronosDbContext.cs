using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence;

// Exists only so Sqlite gets its own migrations history, separate from SqlServer/PostgreSql --
// see ChronosDbContext's constructor comment. Registered in DI as the concrete implementation of
// ChronosDbContext when DatabaseProvider is Sqlite (AddChronosInfrastructure); everything else
// (repositories, etc.) keeps depending on the base ChronosDbContext type.
public class SqliteChronosDbContext : ChronosDbContext
{
    public SqliteChronosDbContext(DbContextOptions<SqliteChronosDbContext> options) : base(options) { }
}
