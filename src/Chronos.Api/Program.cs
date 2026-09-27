using Chronos.Api.Endpoints;
using Chronos.Application.Projects.Commands.CreateProject;
using Chronos.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChronosInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(CreateProjectCommand).Assembly);

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.MapProjectEndpoints();

app.Run();

// Exposed so WebApplicationFactory<Program> can boot this app in-memory for integration tests.
public partial class Program { }
