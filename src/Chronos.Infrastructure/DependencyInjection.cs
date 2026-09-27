using Chronos.Application.Common;
using Chronos.Application.Leave;
using Chronos.Application.Organizations;
using Chronos.Application.Projects;
using Chronos.Application.Users;
using Chronos.Infrastructure.Persistence;
using Chronos.Infrastructure.Persistence.Repositories;
using Chronos.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chronos.Infrastructure;

public static class DependencyInjection
{
    // Single switch point for the multi-DB requirement: everything upstream (Application, Api)
    // depends only on ChronosDbContext / the repository interfaces (DIP), never on the provider.
    public static IServiceCollection AddChronosInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<DatabaseProvider>("DatabaseProvider");
        var connectionString = configuration.GetConnectionString(provider.ToString())
            ?? throw new InvalidOperationException($"Missing connection string for provider '{provider}'.");

        // Each provider gets its own DbContext subclass purely so it can own its own migrations
        // history (see ChronosDbContext's constructor comment) -- but everything upstream still
        // resolves the base ChronosDbContext type via DI, never the concrete subclass.
        switch (provider)
        {
            case DatabaseProvider.SqlServer:
                services.AddDbContext<ChronosDbContext, SqlServerChronosDbContext>(options => options.UseSqlServer(connectionString));
                break;
            case DatabaseProvider.Sqlite:
                services.AddDbContext<ChronosDbContext, SqliteChronosDbContext>(options => options.UseSqlite(connectionString));
                break;
            case DatabaseProvider.PostgreSql:
                services.AddDbContext<ChronosDbContext, PostgreSqlChronosDbContext>(options => options.UseNpgsql(connectionString));
                break;
            default:
                throw new NotSupportedException($"Database provider '{provider}' is not supported.");
        }

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRightRepository, RightRepository>();
        services.AddScoped<IPasswordHistoryRepository, PasswordHistoryRepository>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
