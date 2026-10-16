using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Registration.Application.Accounts.Sessions;

namespace Registration.Api.Security;

public sealed class OpaqueBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, SessionAccess sessions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (header.Length == 0) return AuthenticateResult.NoResult();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Expected a Bearer session token.");

        var owner = await sessions.AuthenticateAsync(header[7..], Context.RequestAborted);
        if (owner is null) return AuthenticateResult.Fail("Invalid or expired session.");

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", owner.Value.Value.ToString("D"))], Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Bearer";
        await base.HandleChallengeAsync(properties);
    }
}
