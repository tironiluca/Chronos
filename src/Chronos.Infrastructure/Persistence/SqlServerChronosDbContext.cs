using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence;

// Exists only so SqlServer gets its own migrations history, separate from Sqlite/PostgreSql --
// see ChronosDbContext's constructor comment. Registered in DI as the concrete implementation of
// ChronosDbContext when DatabaseProvider is SqlServer (AddChronosInfrastructure); everything else
// (repositories, etc.) keeps depending on the base ChronosDbContext type.
public class SqlServerChronosDbContext : ChronosDbContext
{
    public SqlServerChronosDbContext(DbContextOptions<SqlServerChronosDbContext> options) : base(options) { }
}
