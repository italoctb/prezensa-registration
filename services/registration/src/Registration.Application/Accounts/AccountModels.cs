using Registration.Domain.Drafts;

namespace Registration.Application.Accounts;

public enum EmailCodePurpose { CreateAccount, Login }

public sealed record UserAccount(OwnerId Id, CorporateEmail Email, DateTimeOffset CreatedAt);

public sealed record EmailCodeChallenge(
    CorporateEmail Email, EmailCodePurpose Purpose, string CodeHash,
    DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt);

public sealed record AccountSession(
    string TokenHash, OwnerId OwnerId, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

public sealed record SessionIssued(OwnerId OwnerId, string AccessToken, DateTimeOffset ExpiresAt);

public static class AccountErrorCodes
{
    public const string InvalidCorporateEmail = "account.invalid_corporate_email";
    public const string InvalidCode = "account.invalid_code";
    public const string AccountAlreadyExists = "account.already_exists";
}
