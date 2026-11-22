using Microsoft.EntityFrameworkCore;

namespace DeployRequests.Api.Data;

/// <summary>
/// Applies EF Core migrations at startup with a bounded number of attempts,
/// so the app tolerates a database that is still becoming reachable (DNS,
/// network, server start-up) right after `terraform apply`.
/// If every attempt fails, the exception is rethrown and the process exits:
/// the container platform restarts it instead of serving a broken app.
/// EF Core takes a database lock during migration, so several replicas can
/// start at the same time safely.
/// </summary>
public static class DatabaseMigrator
{
    public static async Task MigrateAsync(
        IServiceProvider services,
        DatabaseOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(options.MigrationRetryDelaySeconds);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await db.Database.MigrateAsync(cancellationToken);

                logger.LogInformation("Database migrations applied (attempt {Attempt}/{MaxAttempts}).", attempt, options.MigrationMaxAttempts);
                return;
            }
            catch (Exception ex) when (attempt < options.MigrationMaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Database not ready (attempt {Attempt}/{MaxAttempts}); retrying in {DelaySeconds}s.",
                    attempt,
                    options.MigrationMaxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
