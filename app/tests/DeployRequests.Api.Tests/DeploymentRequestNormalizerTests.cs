using DeployRequests.Api.Features.DeploymentRequests;

namespace DeployRequests.Api.Tests;

public sealed class DeploymentRequestNormalizerTests
{
    [Fact]
    public void Normalizes_ApplicationEnvironmentAndVersion()
    {
        var ok = DeploymentRequestNormalizer.TryNormalize(
            new CreateDeploymentRequest("  Billing-API ", " PRODUCTION ", " 2.0.0-RC1 "),
            out var result,
            out var errors);

        Assert.True(ok);
        Assert.Empty(errors);
        Assert.Equal("Billing-API", result!.Application);
        Assert.Equal("billing-api", result.ApplicationNormalized);
        Assert.Equal("production", result.Environment);
        Assert.Equal("2.0.0-RC1", result.Version); // case preserved
    }

    [Theory]
    [InlineData("staging")]
    [InlineData("production")]
    [InlineData("Staging")]
    public void Accepts_AllowedEnvironments(string environment)
    {
        Assert.True(DeploymentRequestNormalizer.TryNormalize(new CreateDeploymentRequest("app", environment, "1"), out _, out _));
    }

    [Theory]
    [InlineData("dev")]
    [InlineData("staging2")]
    [InlineData("")]
    public void Rejects_OtherEnvironments(string environment)
    {
        Assert.False(DeploymentRequestNormalizer.TryNormalize(new CreateDeploymentRequest("app", environment, "1"), out _, out var errors));
        Assert.True(errors.ContainsKey("environment"));
    }
}
