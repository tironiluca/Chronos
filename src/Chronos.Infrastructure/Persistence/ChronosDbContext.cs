using Chronos.Domain.Leave;
using Chronos.Domain.Organizations;
using Chronos.Domain.Projects;
using Chronos.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence;

public class ChronosDbContext : DbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<User> Users => Set<User>();

    // Non-generic DbContextOptions -- not DbContextOptions&lt;ChronosDbContext&gt; -- so the
    // provider-specific subclasses below (each with their own DbContextOptions&lt;TSelf&gt;) can pass
    // their options straight through. See those classes for why they exist: EF Core migrations
    // are provider-specific, and a single DbContext type can only own one migrations history, so
    // each provider needs its own subclass purely to give its migrations somewhere to live.
    public ChronosDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChronosDbContext).Assembly);
    }
}
