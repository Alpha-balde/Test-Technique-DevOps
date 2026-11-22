using System.Diagnostics.CodeAnalysis;
using DeployRequests.Api.Data;

namespace DeployRequests.Api.Features.DeploymentRequests;

public sealed record NormalizedDeploymentRequest(
    string Application,
    string ApplicationNormalized,
    string Environment,
    string Version);

/// <summary>
/// Normalization and validation rules:
/// <list type="bullet">
/// <item>application: trimmed; compared case-insensitively through <c>ApplicationNormalized</c>; original casing kept for display.</item>
/// <item>environment: trimmed and lower-cased; must be "staging" or "production".</item>
/// <item>version: trimmed only; case is preserved and significant.</item>
/// </list>
/// </summary>
public static class DeploymentRequestNormalizer
{
    public static bool TryNormalize(
        CreateDeploymentRequest input,
        [NotNullWhen(true)] out NormalizedDeploymentRequest? result,
        out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        var application = input.Application?.Trim() ?? string.Empty;
        var environment = input.Environment?.Trim().ToLowerInvariant() ?? string.Empty;
        var version = input.Version?.Trim() ?? string.Empty;

        if (application.Length == 0)
        {
            errors["application"] = ["L'application est obligatoire."];
        }
        else if (application.Length > DeploymentRequestRules.ApplicationMaxLength)
        {
            errors["application"] = [$"L'application ne doit pas dépasser {DeploymentRequestRules.ApplicationMaxLength} caractères."];
        }

        if (environment.Length == 0)
        {
            errors["environment"] = ["L'environnement est obligatoire."];
        }
        else if (!DeploymentRequestRules.AllowedEnvironments.Contains(environment))
        {
            errors["environment"] = ["L'environnement doit être « staging » ou « production »."];
        }

        if (version.Length == 0)
        {
            errors["version"] = ["La version est obligatoire."];
        }
        else if (version.Length > DeploymentRequestRules.VersionMaxLength)
        {
            errors["version"] = [$"La version ne doit pas dépasser {DeploymentRequestRules.VersionMaxLength} caractères."];
        }

        if (errors.Count > 0)
        {
            result = null;
            return false;
        }

        result = new NormalizedDeploymentRequest(
            Application: application,
            ApplicationNormalized: application.ToLowerInvariant(),
            Environment: environment,
            Version: version);
        return true;
    }
}
