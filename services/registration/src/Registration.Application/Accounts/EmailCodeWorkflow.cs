using Registration.Application.Abstractions.Integrations;
using Registration.Application.Abstractions.Persistence;

namespace Registration.Application.Accounts;

public sealed class EmailCodeWorkflow(
    IAccountRepository accounts, IEmailCodeRepository codes,
    IAccountSessionRepository sessions, IAuthenticationEmailSender sender,
    AuthenticationPolicy policy, TimeProvider timeProvider)
{
    public async Task RequestAsync(string? value, EmailCodePurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var email = CorporateEmail.Create(value);
        var account = await accounts.FindByEmailAsync(email, cancellationToken);
        if ((purpose == EmailCodePurpose.CreateAccount && account is not null) ||
            (purpose == EmailCodePurpose.Login && account is null))
            return;

        var now = timeProvider.GetUtcNow();
        var code = AuthenticationPolicy.NewCode();
        var challenge = new EmailCodeChallenge(email, purpose, policy.HashCode(email, purpose, code),
            now, now.Add(policy.CodeLifetime));
        if (!await codes.TryIssueAsync(challenge, policy.ResendInterval, policy.MaxFailedAttempts,
                cancellationToken))
            return;

        try
        {
            await sender.SendCodeAsync(email, purpose, code, cancellationToken);
        }
        catch
        {
            await codes.InvalidateAsync(email, purpose, challenge.CodeHash, CancellationToken.None);
            throw;
        }
    }

    public async Task<SessionIssued> ConfirmAsync(string? value, string? code,
        EmailCodePurpose purpose, CancellationToken cancellationToken = default)
    {
        var email = CorporateEmail.Create(value);
        if (code is not { Length: 8 } || code.Any(c => !char.IsAsciiDigit(c)))
            throw InvalidCode();

        var now = timeProvider.GetUtcNow();
        var hash = policy.HashCode(email, purpose, code);
        if (!await codes.TryConsumeAsync(email, purpose, hash, now, policy.MaxFailedAttempts,
                cancellationToken))
            throw InvalidCode();

        UserAccount account;
        if (purpose == EmailCodePurpose.CreateAccount)
        {
            account = new UserAccount(Registration.Domain.Drafts.OwnerId.From(Guid.NewGuid()), email, now);
            if (!await accounts.TryCreateAsync(account, cancellationToken))
                throw new RegistrationApplicationException(AccountErrorCodes.AccountAlreadyExists,
                    "An account already exists for this email.");
        }
        else
        {
            account = await accounts.FindByEmailAsync(email, cancellationToken) ?? throw InvalidCode();
        }

        var (token, tokenHash) = AuthenticationPolicy.NewSessionToken();
        var expiresAt = now.Add(policy.SessionLifetime);
        await sessions.CreateAsync(new AccountSession(tokenHash, account.Id, now, expiresAt),
            cancellationToken);
        return new SessionIssued(account.Id, token, expiresAt);
    }

    private static RegistrationApplicationException InvalidCode() => new(
        AccountErrorCodes.InvalidCode, "The code is invalid or expired.");
}
