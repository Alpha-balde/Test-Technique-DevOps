using System.Net;
using System.Net.Http.Json;
using DeployRequests.Api.Data;
using DeployRequests.Api.Features.DeploymentRequests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DeployRequests.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class DeploymentRequestsApiTests(ApiFixture fixture)
{
    private const string Route = DeploymentRequestEndpoints.RoutePrefix;
    private readonly HttpClient _client = fixture.Client;

    // Each test uses its own application name so tests never interfere.
    private static string UniqueApp(string prefix = "app") => $"{prefix}-{Guid.NewGuid():N}";

    [Fact]
    public async Task Health_ReturnsHealthy_WhenDatabaseIsReachable()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>();
        Assert.Equal("Healthy", body!.Status);
        Assert.Equal("Healthy", body.Checks["database"]);
    }

    [Fact]
    public async Task HealthLive_DoesNotDependOnDatabase()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>();
        Assert.Empty(body!.Checks);
    }

    [Fact]
    public async Task Create_ReturnsCreated_AndRequestAppearsInList()
    {
        var app = UniqueApp("Billing-API");

        var response = await _client.PostAsJsonAsync(Route, new { application = $"  {app}  ", environment = " Staging ", version = " 1.2.0 " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<DeploymentRequestResponse>();
        Assert.NotNull(created);
        Assert.Equal(app, created.Application);           // trimmed, original casing kept
        Assert.Equal("staging", created.Environment);     // lower-cased
        Assert.Equal("1.2.0", created.Version);           // trimmed

        var list = await _client.GetFromJsonAsync<List<DeploymentRequestResponse>>(Route);
        Assert.Contains(list!, r => r.Id == created.Id);
    }

    [Fact]
    public async Task List_ReturnsNewestFirst()
    {
        var app = UniqueApp();
        await PostAsync(app, "staging", "1.0.0");
        await PostAsync(app, "staging", "2.0.0");

        var list = await _client.GetFromJsonAsync<List<DeploymentRequestResponse>>(Route);
        var mine = list!.Where(r => r.Application == app).Select(r => r.Version).ToList();

        Assert.Equal(["2.0.0", "1.0.0"], mine);
    }

    [Fact]
    public async Task Create_Duplicate_IsRejectedWith409AndClearMessage()
    {
        var app = UniqueApp("Billing-API");
        Assert.Equal(HttpStatusCode.Created, (await PostAsync(app, "staging", "1.2.0")).StatusCode);

        // Same triplet with different application casing and surrounding spaces.
        var duplicate = await PostAsync($"  {app.ToUpperInvariant()} ", " STAGING", "1.2.0 ");

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Equal("Demande déjà enregistrée", problem.Title);
        Assert.Contains("existe déjà", problem.Detail);
        Assert.Contains("staging", problem.Detail);
        Assert.Contains("1.2.0", problem.Detail);
    }

    [Fact]
    public async Task Create_SameApplicationAndVersion_InOtherEnvironment_IsAccepted()
    {
        var app = UniqueApp();

        Assert.Equal(HttpStatusCode.Created, (await PostAsync(app, "staging", "3.0.0")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PostAsync(app, "production", "3.0.0")).StatusCode);
    }

    [Fact]
    public async Task Create_VersionIsCaseSensitive()
    {
        var app = UniqueApp();

        Assert.Equal(HttpStatusCode.Created, (await PostAsync(app, "production", "1.0.0-RC1")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PostAsync(app, "production", "1.0.0-rc1")).StatusCode);
    }

    [Fact]
    public async Task Create_ConcurrentDuplicates_OnlyOneSucceeds()
    {
        var app = UniqueApp();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => PostAsync(app, "production", "9.9.9")));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Theory]
    [InlineData("dev")]
    [InlineData("prod")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Create_InvalidEnvironment_Returns400(string? environment)
    {
        var response = await PostAsync(UniqueApp(), environment, "1.0.0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("environment"));
    }

    [Fact]
    public async Task Create_MissingFields_ReportsEveryField()
    {
        var response = await _client.PostAsJsonAsync(Route, new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Equal(["application", "environment", "version"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task Create_TooLongValues_Returns400()
    {
        var response = await PostAsync(new string('a', DeploymentRequestRules.ApplicationMaxLength + 1), "staging", new string('1', DeploymentRequestRules.VersionMaxLength + 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("application"));
        Assert.True(problem.Errors.ContainsKey("version"));
    }

    [Fact]
    public async Task Database_IsFullyMigrated_AndMigrationsMatchModel()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_InitialCreate"));
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private Task<HttpResponseMessage> PostAsync(string? application, string? environment, string? version) =>
        _client.PostAsJsonAsync(Route, new CreateDeploymentRequest(application, environment, version));

    private sealed record HealthBody(string Status, Dictionary<string, string> Checks);
}
