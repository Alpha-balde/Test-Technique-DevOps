using DeployRequests.Api.Data;
using DeployRequests.Api.Features.DeploymentRequests;
using DeployRequests.Api.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// The connection string is read lazily from configuration
// (env var ConnectionStrings__Database in containers, user-secrets locally).
// It is never stored in the repository.
builder.Services.AddDbContext<AppDbContext>((services, options) =>
{
    var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("Database");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'ConnectionStrings:Database' is not configured " +
            "(set the ConnectionStrings__Database environment variable).");
    }

    options.UseNpgsql(connectionString, npgsql =>
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null));
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// /health: full check including database connectivity (used for the demo / readiness).
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync });

// /health/live: process-only check, no dependency. Used as liveness probe so a
// transient database outage never triggers a container restart loop.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteAsync,
});

app.MapDeploymentRequestEndpoints();

var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
if (databaseOptions.MigrateOnStartup)
{
    await DatabaseMigrator.MigrateAsync(app.Services, databaseOptions, app.Logger, app.Lifetime.ApplicationStopping);
}

await app.RunAsync();

// Exposes the entry point to WebApplicationFactory in integration tests.
public partial class Program { }
