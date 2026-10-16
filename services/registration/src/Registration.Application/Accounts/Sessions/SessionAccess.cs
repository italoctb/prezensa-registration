using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Drafts;

namespace Registration.Application.Accounts.Sessions;

public sealed class SessionAccess(IAccountSessionRepository sessions, TimeProvider timeProvider)
{
    public async Task<OwnerId?> AuthenticateAsync(string? token,
        CancellationToken cancellationToken = default)
    {
        if (token is not { Length: 43 } || token.Any(c =>
                !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_')))
            return null;

        var hash = AuthenticationPolicy.HashSessionToken(token);
        var session = await sessions.FindActiveAsync(hash, timeProvider.GetUtcNow(), cancellationToken);
        return session?.OwnerId;
    }

    public Task LogoutAsync(string token, CancellationToken cancellationToken = default) =>
        sessions.RevokeAsync(AuthenticationPolicy.HashSessionToken(token), cancellationToken);
}
