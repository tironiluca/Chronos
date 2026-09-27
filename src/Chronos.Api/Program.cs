using System.Text;
using System.Text.Json.Serialization;
using Chronos.Api.Endpoints;
using Chronos.Application.Projects.Commands.CreateProject;
using Chronos.Domain.Users;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChronosInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(CreateProjectCommand).Assembly);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"Missing '{JwtOptions.SectionName}' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Only Approvers/Admins may approve or reject a leave request -- an Employee can request
    // and cancel their own, never someone else's, and never approve anything.
    options.AddPolicy("ApproverOrAdmin", policy =>
        policy.RequireRole(nameof(UserRole.Approver), nameof(UserRole.Admin)));
});

// Endpoint request/response records use enums (e.g. LeaveType) by their string name over the
// wire -- without this, System.Text.Json only accepts their numeric value, and every request
// sending e.g. "Vacation" fails deserialization with a 400 before it ever reaches a handler.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// No EF Core migrations exist yet (see IMPLEMENTATION_PLAN.md) -- EnsureCreated stands in for
// them across all three providers so the schema actually exists before the first request. Swap
// this for a Database.Migrate() call once migrations are introduced.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Chronos.Infrastructure.Persistence.ChronosDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapProjectEndpoints();
app.MapLeaveEndpoints();

app.Run();

// Exposed so WebApplicationFactory<Program> can boot this app in-memory for integration tests.
public partial class Program { }
