using Registration.Application.Accounts;

namespace Registration.Api.Contracts;

public sealed record EmailRequest(string? Email);
public sealed record ConfirmEmailCodeRequest(string? Email, string? Code);
public sealed record SessionResponse(Guid OwnerId, string AccessToken, string TokenType,
    DateTimeOffset ExpiresAt)
{
    public static SessionResponse From(SessionIssued issued) =>
        new(issued.OwnerId.Value, issued.AccessToken, "Bearer", issued.ExpiresAt);
}
