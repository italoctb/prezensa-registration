using Registration.Api.Contracts;
using Registration.Api.Security;
using Registration.Application.Registrations.SubmitDraft;
using Registration.Domain.Drafts;

namespace Registration.Api.Endpoints;

public static class SubmitDraftEndpoints
{
    public static IEndpointRouteBuilder MapSubmitDraftEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/drafts/{draftId:guid}/submit", Submit).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> Submit(Guid draftId, HttpContext context,
        SubmitRegistrationDraftHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(RegistrationDraftId.From(draftId),
            OwnerIdentity.From(context.User)), cancellationToken);
        return Results.Ok(new SubmitDraftResponse(result.RegistrationId.Value));
    }
}
