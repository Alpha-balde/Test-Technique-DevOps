using DeployRequests.Api.Data;
using DeployRequests.Api.Features.DeploymentRequests;
using DeployRequests.Api.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

// The DbContext check goes through the Npgsql retry strategy, which could make
// /health hang ~30s when the database is down. Cap it so probes get a fast answer.
builder.Services.Configure<HealthCheckServiceOptions>(options =>
{
    foreach (var registration in options.Registrations)
    {
        registration.Timeout = TimeSpan.FromSeconds(3);
    }
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// The Angular build is copied into wwwroot by the Dockerfile: one image, one
// origin, no CORS. Single screen, so no SPA fallback route is needed.
// index.html is revalidated on every load; hashed bundles can be cached.
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "no-cache";
        }
    },
});

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
