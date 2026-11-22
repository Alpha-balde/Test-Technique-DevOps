using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DeployRequests.Api.Data;

/// <summary>
/// Used only by the `dotnet ef` tooling to create migrations. Generating a
/// migration does not open a connection, so no real credentials are needed.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time_only")
            .Options;

        return new AppDbContext(options);
    }
}
