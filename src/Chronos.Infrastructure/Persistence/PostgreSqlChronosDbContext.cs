using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence;

// Exists only so PostgreSql gets its own migrations history, separate from SqlServer/Sqlite --
// see ChronosDbContext's constructor comment. Registered in DI as the concrete implementation of
// ChronosDbContext when DatabaseProvider is PostgreSql (AddChronosInfrastructure); everything else
// (repositories, etc.) keeps depending on the base ChronosDbContext type.
public class PostgreSqlChronosDbContext : ChronosDbContext
{
    public PostgreSqlChronosDbContext(DbContextOptions<PostgreSqlChronosDbContext> options) : base(options) { }
}
