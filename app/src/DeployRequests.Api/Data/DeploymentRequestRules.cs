namespace DeployRequests.Api.Data;

/// <summary>
/// Business constants shared by validation and the database schema.
/// </summary>
public static class DeploymentRequestRules
{
    public const int ApplicationMaxLength = 100;
    public const int VersionMaxLength = 50;
    public const int EnvironmentMaxLength = 20;

    public const string Staging = "staging";
    public const string Production = "production";

    public static readonly IReadOnlyList<string> AllowedEnvironments = [Staging, Production];
}
