using Chronos.Domain.Leave;
using Chronos.Domain.Organizations;
using Chronos.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure.Persistence;

public class ChronosDbContext : DbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public ChronosDbContext(DbContextOptions<ChronosDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChronosDbContext).Assembly);
    }
}
