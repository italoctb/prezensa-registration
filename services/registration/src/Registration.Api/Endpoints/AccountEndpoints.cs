using Registration.Api.Contracts;
using Registration.Application.Accounts.Login;
using Registration.Application.Accounts.Register;
using Registration.Application.Accounts.Sessions;

namespace Registration.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/accounts/codes", RequestAccountCode).RequireRateLimiting("account-access");
        app.MapPost("/accounts", ConfirmAccount).RequireRateLimiting("account-access");
        app.MapPost("/sessions/codes", RequestLoginCode).RequireRateLimiting("account-access");
        app.MapPost("/sessions", ConfirmLogin).RequireRateLimiting("account-access");
        app.MapDelete("/sessions/current", Logout).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> RequestAccountCode(EmailRequest request,
        RequestAccountCodeHandler handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new(request.Email), cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> ConfirmAccount(ConfirmEmailCodeRequest request,
        ConfirmAccountHandler handler, CancellationToken cancellationToken)
    {
        var issued = await handler.HandleAsync(new(request.Email, request.Code), cancellationToken);
        return Results.Json(SessionResponse.From(issued), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> RequestLoginCode(EmailRequest request,
        RequestLoginCodeHandler handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new(request.Email), cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> ConfirmLogin(ConfirmEmailCodeRequest request,
        ConfirmLoginHandler handler, CancellationToken cancellationToken)
    {
        var issued = await handler.HandleAsync(new(request.Email, request.Code), cancellationToken);
        return Results.Ok(SessionResponse.From(issued));
    }

    private static async Task<IResult> Logout(HttpContext context, SessionAccess sessions,
        CancellationToken cancellationToken)
    {
        await sessions.LogoutAsync(context.Request.Headers.Authorization.ToString()[7..], cancellationToken);
        return Results.NoContent();
    }
}
