using Registration.Application.Abstractions.Integrations;
using Registration.Application.Abstractions.Persistence;
using Registration.Application.Accounts;
using Registration.Domain.Drafts;

namespace Registration.Api.Tests.Support;

public sealed class FakeAccountAccessStore : IAccountRepository, IEmailCodeRepository,
    IAccountSessionRepository, IAuthenticationEmailSender
{
    private readonly Dictionary<string, UserAccount> _accounts = [];
    private readonly Dictionary<string, Challenge> _codes = [];
    private readonly Dictionary<string, AccountSession> _sessions = [];
    private readonly Dictionary<string, string> _sentCodes = [];
    private readonly object _gate = new();

    public Task<UserAccount?> FindByEmailAsync(CorporateEmail email, CancellationToken cancellationToken)
    {
        lock (_gate) return Task.FromResult(_accounts.GetValueOrDefault(email.Value));
    }

    public Task<bool> TryCreateAsync(UserAccount account, CancellationToken cancellationToken)
    {
        lock (_gate) return Task.FromResult(_accounts.TryAdd(account.Email.Value, account));
    }

    public Task<bool> TryIssueAsync(EmailCodeChallenge challenge, TimeSpan resendInterval,
        int maxFailedAttempts, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var key = Key(challenge.Email, challenge.Purpose);
            if (_codes.TryGetValue(key, out var old) && old.ExpiresAt > challenge.IssuedAt &&
                (old.IssuedAt.Add(resendInterval) > challenge.IssuedAt ||
                 old.FailedAttempts >= maxFailedAttempts))
                return Task.FromResult(false);
            _codes[key] = new Challenge(challenge.CodeHash, challenge.IssuedAt,
                challenge.ExpiresAt, old is not null && old.ExpiresAt > challenge.IssuedAt
                    ? old.FailedAttempts : 0);
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryConsumeAsync(CorporateEmail email, EmailCodePurpose purpose,
        string codeHash, DateTimeOffset now, int maxFailedAttempts, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_codes.TryGetValue(Key(email, purpose), out var challenge) ||
                challenge.ExpiresAt <= now || challenge.Consumed ||
                challenge.FailedAttempts >= maxFailedAttempts)
                return Task.FromResult(false);
            if (challenge.Hash != codeHash)
            {
                challenge.FailedAttempts++;
                return Task.FromResult(false);
            }
            challenge.Consumed = true;
            return Task.FromResult(true);
        }
    }

    public Task InvalidateAsync(CorporateEmail email, EmailCodePurpose purpose,
        string codeHash, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var key = Key(email, purpose);
            if (_codes.TryGetValue(key, out var challenge) && challenge.Hash == codeHash)
                challenge.ExpiresAt = DateTimeOffset.UnixEpoch;
        }
        return Task.CompletedTask;
    }

    public Task SendCodeAsync(CorporateEmail email, EmailCodePurpose purpose, string code,
        CancellationToken cancellationToken)
    {
        lock (_gate) _sentCodes[Key(email, purpose)] = code;
        return Task.CompletedTask;
    }

    public string? SentCode(string email, EmailCodePurpose purpose)
    {
        lock (_gate) return _sentCodes.GetValueOrDefault(Key(CorporateEmail.Create(email), purpose));
    }

    public string SeedSession(OwnerId owner, DateTimeOffset expiresAt)
    {
        var (token, hash) = AuthenticationPolicy.NewSessionToken();
        lock (_gate) _sessions.Add(hash, new AccountSession(hash, owner,
            expiresAt.AddHours(-12), expiresAt));
        return token;
    }

    public Task CreateAsync(AccountSession session, CancellationToken cancellationToken)
    {
        lock (_gate) _sessions.Add(session.TokenHash, session);
        return Task.CompletedTask;
    }

    public Task<AccountSession?> FindActiveAsync(string tokenHash, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var session = _sessions.GetValueOrDefault(tokenHash);
            return Task.FromResult(session is not null && session.ExpiresAt > now ? session : null);
        }
    }

    public Task RevokeAsync(string tokenHash, CancellationToken cancellationToken)
    {
        lock (_gate) _sessions.Remove(tokenHash);
        return Task.CompletedTask;
    }

    private static string Key(CorporateEmail email, EmailCodePurpose purpose) =>
        $"{purpose}:{email.Value}";

    private sealed class Challenge(string hash, DateTimeOffset issuedAt,
        DateTimeOffset expiresAt, int failedAttempts)
    {
        public string Hash { get; } = hash;
        public DateTimeOffset IssuedAt { get; } = issuedAt;
        public DateTimeOffset ExpiresAt { get; set; } = expiresAt;
        public int FailedAttempts { get; set; } = failedAttempts;
        public bool Consumed { get; set; }
    }
}
