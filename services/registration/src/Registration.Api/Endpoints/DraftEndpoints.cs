using Registration.Api.Contracts;
using Registration.Api.Security;
using Registration.Application.Drafts.GetCurrent;
using Registration.Application.Drafts.Start;

namespace Registration.Api.Endpoints;

public static class DraftEndpoints
{
    public static IEndpointRouteBuilder MapDraftEndpoints(this IEndpointRouteBuilder app)
    {
        var drafts = app.MapGroup("/drafts").RequireAuthorization();
        drafts.MapPost("", Start);
        drafts.MapGet("/current", GetCurrent);
        drafts.MapDraftUpdateEndpoints();
        return app;
    }

    private static async Task<IResult> Start(HttpContext context, StartDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(OwnerIdentity.From(context.User)), cancellationToken);
        return Results.Ok(DraftResponse.From(result));
    }

    private static async Task<IResult> GetCurrent(HttpContext context, GetCurrentDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(OwnerIdentity.From(context.User)), cancellationToken);
        return result is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Active draft not found",
                extensions: new Dictionary<string, object?> { ["code"] = "registration.draft_not_found" })
            : Results.Ok(DraftResponse.From(result));
    }
}
