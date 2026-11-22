namespace DeployRequests.Api.Data;

/// <summary>
/// A request to deploy a given version of an application to an environment.
/// </summary>
public sealed class DeploymentRequest
{
    public Guid Id { get; init; }

    /// <summary>Application name as typed by the user (trimmed), kept for display.</summary>
    public required string Application { get; init; }

    /// <summary>Trimmed, lower-cased application name used by the uniqueness rule.</summary>
    public required string ApplicationNormalized { get; init; }

    /// <summary>Either "staging" or "production" (lower-case).</summary>
    public required string Environment { get; init; }

    /// <summary>Trimmed version, case preserved (case-sensitive in the uniqueness rule).</summary>
    public required string Version { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
