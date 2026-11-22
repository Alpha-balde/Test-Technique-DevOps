using Microsoft.EntityFrameworkCore;

namespace DeployRequests.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Unique index enforcing the business rule: one request per
    /// (normalized application, environment, version). The API maps a
    /// violation of this index to HTTP 409.
    /// </summary>
    public const string UniqueRequestIndexName = "ux_deployment_requests_application_environment_version";

    public const string EnvironmentCheckConstraintName = "ck_deployment_requests_environment";

    public DbSet<DeploymentRequest> DeploymentRequests => Set<DeploymentRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var allowedEnvironments = string.Join(", ", DeploymentRequestRules.AllowedEnvironments.Select(e => $"'{e}'"));

        var entity = modelBuilder.Entity<DeploymentRequest>();

        entity.ToTable("deployment_requests", table =>
            table.HasCheckConstraint(EnvironmentCheckConstraintName, $"environment IN ({allowedEnvironments})"));

        entity.HasKey(r => r.Id).HasName("pk_deployment_requests");

        entity.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        entity.Property(r => r.Application)
            .HasColumnName("application")
            .HasMaxLength(DeploymentRequestRules.ApplicationMaxLength)
            .IsRequired();

        entity.Property(r => r.ApplicationNormalized)
            .HasColumnName("application_normalized")
            .HasMaxLength(DeploymentRequestRules.ApplicationMaxLength)
            .IsRequired();

        entity.Property(r => r.Environment)
            .HasColumnName("environment")
            .HasMaxLength(DeploymentRequestRules.EnvironmentMaxLength)
            .IsRequired();

        entity.Property(r => r.Version)
            .HasColumnName("version")
            .HasMaxLength(DeploymentRequestRules.VersionMaxLength)
            .IsRequired();

        entity.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        entity.HasIndex(r => new { r.ApplicationNormalized, r.Environment, r.Version })
            .IsUnique()
            .HasDatabaseName(UniqueRequestIndexName);
    }
}
