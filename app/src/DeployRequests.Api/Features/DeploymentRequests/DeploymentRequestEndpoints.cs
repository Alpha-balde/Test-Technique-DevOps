using DeployRequests.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DeployRequests.Api.Features.DeploymentRequests;

public static class DeploymentRequestEndpoints
{
    public const string RoutePrefix = "/api/deployment-requests";

    public static IEndpointRouteBuilder MapDeploymentRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix);

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);

        return app;
    }

    private static async Task<Ok<List<DeploymentRequestResponse>>> ListAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var requests = await db.DeploymentRequests
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new DeploymentRequestResponse(r.Id, r.Application, r.Environment, r.Version, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(requests);
    }

    private static async Task<Results<Created<DeploymentRequestResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CreateDeploymentRequest input,
        AppDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (!DeploymentRequestNormalizer.TryNormalize(input, out var request, out var errors))
        {
            return TypedResults.ValidationProblem(errors, title: "La demande est invalide.");
        }

        var entity = new DeploymentRequest
        {
            Id = Guid.CreateVersion7(),
            Application = request.Application,
            ApplicationNormalized = request.ApplicationNormalized,
            Environment = request.Environment,
            Version = request.Version,
            CreatedAt = clock.GetUtcNow(),
        };

        db.DeploymentRequests.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateRequest(ex))
        {
            // The unique index is the single source of truth: it also covers
            // concurrent submissions that a "check then insert" would miss.
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Demande déjà enregistrée",
                detail: $"Une demande existe déjà pour l'application « {request.Application} », " +
                        $"l'environnement « {request.Environment} » et la version « {request.Version} ».");
        }

        var response = new DeploymentRequestResponse(entity.Id, entity.Application, entity.Environment, entity.Version, entity.CreatedAt);
        return TypedResults.Created((string?)null, response);
    }

    private static bool IsDuplicateRequest(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: AppDbContext.UniqueRequestIndexName,
        };
}
