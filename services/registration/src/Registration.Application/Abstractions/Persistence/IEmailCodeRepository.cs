using Registration.Application.Accounts;

namespace Registration.Application.Abstractions.Persistence;

public interface IEmailCodeRepository
{
    Task<bool> TryIssueAsync(EmailCodeChallenge challenge, TimeSpan resendInterval,
        int maxFailedAttempts, CancellationToken cancellationToken);
    Task<bool> TryConsumeAsync(CorporateEmail email, EmailCodePurpose purpose, string codeHash,
        DateTimeOffset now, int maxFailedAttempts, CancellationToken cancellationToken);
    Task InvalidateAsync(CorporateEmail email, EmailCodePurpose purpose, string codeHash,
        CancellationToken cancellationToken);
}
