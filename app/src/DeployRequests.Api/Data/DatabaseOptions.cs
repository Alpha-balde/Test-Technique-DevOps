using System.ComponentModel.DataAnnotations;

namespace DeployRequests.Api.Data;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Apply pending EF Core migrations when the application starts.</summary>
    public bool MigrateOnStartup { get; set; } = true;

    /// <summary>Maximum attempts to reach the database and migrate before giving up.</summary>
    [Range(1, 60)]
    public int MigrationMaxAttempts { get; set; } = 10;

    /// <summary>Delay between two migration attempts.</summary>
    [Range(1, 60)]
    public int MigrationRetryDelaySeconds { get; set; } = 6;
}
