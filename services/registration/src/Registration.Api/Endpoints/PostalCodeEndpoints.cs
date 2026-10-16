using Registration.Api.Contracts;
using Registration.Application.Drafts.LookupPostalCode;

namespace Registration.Api.Endpoints;

public static class PostalCodeEndpoints
{
    public static IEndpointRouteBuilder MapPostalCodeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/postal-codes/{postalCode}", Lookup).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> Lookup(string postalCode, LookupPostalCodeHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(postalCode), cancellationToken);
        return Results.Ok(PostalCodeResponse.From(result));
    }
}
