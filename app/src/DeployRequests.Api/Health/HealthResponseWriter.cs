using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DeployRequests.Api.Health;

/// <summary>
/// Writes a small JSON health report. Exception details are deliberately
/// not exposed: only check names and statuses are returned.
/// </summary>
public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds),
            checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString()),
        });
}
