namespace DeployRequests.Api.Features.DeploymentRequests;

/// <summary>Body of POST /api/deployment-requests. Fields are nullable so validation can report every missing field.</summary>
public sealed record CreateDeploymentRequest(string? Application, string? Environment, string? Version);

public sealed record DeploymentRequestResponse(
    Guid Id,
    string Application,
    string Environment,
    string Version,
    DateTimeOffset CreatedAt);
