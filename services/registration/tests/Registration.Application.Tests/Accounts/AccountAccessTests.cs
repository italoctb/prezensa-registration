using Registration.Application;
using Registration.Application.Abstractions.Integrations;
using Registration.Application.Abstractions.Persistence;
using Registration.Application.Accounts;
using Registration.Application.Accounts.Sessions;
using Registration.Domain.Drafts;

namespace Registration.Application.Tests.Accounts;

public sealed class AccountAccessTests
{
    [Theory]
    [InlineData(" Ana.Silva@PREZENSA.COM ", "ana.silva@prezensa.com")]
    [InlineData("a+b@prezensa.com", "a+b@prezensa.com")]
    public void Corporate_email_is_canonical(string input, string expected)
    {
        Assert.Equal(expected, CorporateEmail.Create(input).Value);
        Assert.Equal(CorporateEmail.Create(expected), CorporateEmail.Create(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("ana@example.com")]
    [InlineData("ana@sub.prezensa.com")]
    [InlineData("ana@prezensa.com.evil.test")]
    [InlineData("a b@prezensa.com")]
    public void Other_emails_cannot_identify_an_account(string? input) =>
        Assert.Equal(AccountErrorCodes.InvalidCorporateEmail,
            Assert.Throws<RegistrationApplicationException>(() => CorporateEmail.Create(input)).Code);

    [Fact]
    public async Task Registration_and_login_are_distinct_and_issue_revocable_sessions()
    {
        var test = new Fixture();
        var email = CorporateEmail.Create("ana@prezensa.com");
        await test.Workflow.RequestAsync(email.Value, EmailCodePurpose.CreateAccount);
        var signupCode = test.Store.SentCode!;
        Assert.Matches("^[0-9]{8}$", signupCode);
        Assert.NotEqual(signupCode, test.Store.LastChallenge!.CodeHash);

        var registered = await test.Workflow.ConfirmAsync(email.Value, signupCode,
            EmailCodePurpose.CreateAccount);
        Assert.NotEqual(Guid.Empty, registered.OwnerId.Value);
        Assert.Equal(registered.OwnerId,
            await test.Sessions.AuthenticateAsync(registered.AccessToken));
        await test.Sessions.LogoutAsync(registered.AccessToken);
        Assert.Null(await test.Sessions.AuthenticateAsync(registered.AccessToken));

        await test.Workflow.RequestAsync(email.Value, EmailCodePurpose.Login);
        var loggedIn = await test.Workflow.ConfirmAsync(email.Value, test.Store.SentCode,
            EmailCodePurpose.Login);
        Assert.Equal(registered.OwnerId, loggedIn.OwnerId);
        Assert.NotEqual(registered.AccessToken, loggedIn.AccessToken);
    }

    [Fact]
    public async Task A_code_cannot_be_reused_or_used_for_a_different_purpose()
    {
        var test = new Fixture();
        await test.Workflow.RequestAsync("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        var code = test.Store.SentCode;
        await AssertInvalidCode(() => test.Workflow.ConfirmAsync("ana@prezensa.com", code,
            EmailCodePurpose.Login));
        await test.Workflow.ConfirmAsync("ana@prezensa.com", code, EmailCodePurpose.CreateAccount);
        await AssertInvalidCode(() => test.Workflow.ConfirmAsync("ana@prezensa.com", code,
            EmailCodePurpose.CreateAccount));
    }

    [Fact]
    public async Task Unknown_login_does_not_send_a_code_and_signup_of_existing_account_is_silent()
    {
        var test = new Fixture();
        await test.Workflow.RequestAsync("ana@prezensa.com", EmailCodePurpose.Login);
        Assert.Null(test.Store.SentCode);
        await test.Workflow.RequestAsync("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        await test.Workflow.ConfirmAsync("ana@prezensa.com", test.Store.SentCode,
            EmailCodePurpose.CreateAccount);
        test.Store.SentCode = null;
        await test.Workflow.RequestAsync("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        Assert.Null(test.Store.SentCode);
    }

    [Fact]
    public async Task Expired_codes_and_sessions_are_rejected()
    {
        var test = new Fixture();
        await test.Workflow.RequestAsync("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        var code = test.Store.SentCode;
        test.Clock.Advance(TimeSpan.FromMinutes(10));
        await AssertInvalidCode(() => test.Workflow.ConfirmAsync("ana@prezensa.com", code,
            EmailCodePurpose.CreateAccount));
        await test.Workflow.RequestAsync("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        var session = await test.Workflow.ConfirmAsync("ana@prezensa.com", test.Store.SentCode,
            EmailCodePurpose.CreateAccount);
        test.Clock.Advance(TimeSpan.FromHours(12));
        Assert.Null(await test.Sessions.AuthenticateAsync(session.AccessToken));
    }

    [Fact]
    public async Task Five_failures_lock_the_challenge_and_resend_does_not_reset_count()
    {
        var test = new Fixture();
        await test.Workflow.RequestAsync("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        var original = test.Store.SentCode;
        var wrong = original == "00000000" ? "11111111" : "00000000";
        for (var i = 0; i < 4; i++)
            await AssertInvalidCode(() => test.Workflow.ConfirmAsync("ana@prezensa.com",
                wrong, EmailCodePurpose.CreateAccount));
        test.Clock.Advance(TimeSpan.FromMinutes(1));
        await test.Workflow.RequestAsync("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        var replacement = test.Store.SentCode;
        var finalWrong = replacement == "22222222" ? "33333333" : "22222222";
        await AssertInvalidCode(() => test.Workflow.ConfirmAsync("ana@prezensa.com",
            finalWrong, EmailCodePurpose.CreateAccount));
        await AssertInvalidCode(() => test.Workflow.ConfirmAsync("ana@prezensa.com",
            replacement, EmailCodePurpose.CreateAccount));
    }

    [Fact]
    public async Task Delivery_failure_invalidates_challenge()
    {
        var test = new Fixture();
        test.Store.FailDelivery = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => test.Workflow.RequestAsync(
            "ana@prezensa.com", EmailCodePurpose.CreateAccount));
        test.Store.FailDelivery = false;
        await test.Workflow.RequestAsync("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        Assert.NotNull(test.Store.SentCode);
    }

    private static async Task AssertInvalidCode(Func<Task> action) =>
        Assert.Equal(AccountErrorCodes.InvalidCode,
            (await Assert.ThrowsAsync<RegistrationApplicationException>(action)).Code);

    private sealed class Fixture
    {
        public FakeStore Store { get; } = new();
        public MutableTimeProvider Clock { get; } = new();
        public EmailCodeWorkflow Workflow { get; }
        public SessionAccess Sessions { get; }

        public Fixture()
        {
            Workflow = new EmailCodeWorkflow(Store, Store, Store, Store,
                new AuthenticationPolicy(new byte[32]), Clock);
            Sessions = new SessionAccess(Store, Clock);
        }
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }

    private sealed class FakeStore : IAccountRepository, IEmailCodeRepository,
        IAccountSessionRepository, IAuthenticationEmailSender
    {
        private readonly Dictionary<string, UserAccount> _accounts = [];
        private readonly Dictionary<string, AccountSession> _sessions = [];
        private readonly Dictionary<EmailCodePurpose, ChallengeState> _challenges = [];
        public EmailCodeChallenge? LastChallenge { get; private set; }
        public string? SentCode { get; set; }
        public bool FailDelivery { get; set; }

        public Task<UserAccount?> FindByEmailAsync(CorporateEmail email, CancellationToken cancellationToken) =>
            Task.FromResult(_accounts.GetValueOrDefault(email.Value));
        public Task<bool> TryCreateAsync(UserAccount account, CancellationToken cancellationToken) =>
            Task.FromResult(_accounts.TryAdd(account.Email.Value, account));
        public Task<bool> TryIssueAsync(EmailCodeChallenge challenge, TimeSpan resendInterval,
            int maxFailedAttempts, CancellationToken cancellationToken)
        {
            _challenges.TryGetValue(challenge.Purpose, out var old);
            if (old is not null && old.Challenge.ExpiresAt > challenge.IssuedAt &&
                (old.Challenge.IssuedAt.Add(resendInterval) > challenge.IssuedAt ||
                 old.Failures >= maxFailedAttempts))
                return Task.FromResult(false);
            _challenges[challenge.Purpose] = new ChallengeState(challenge,
                old is not null && old.Challenge.ExpiresAt > challenge.IssuedAt ? old.Failures : 0);
            LastChallenge = challenge;
            return Task.FromResult(true);
        }
        public Task<bool> TryConsumeAsync(CorporateEmail email, EmailCodePurpose purpose, string codeHash,
            DateTimeOffset now, int maxFailedAttempts, CancellationToken cancellationToken)
        {
            if (!_challenges.TryGetValue(purpose, out var state) ||
                state.Challenge.Email != email || state.Challenge.ExpiresAt <= now ||
                state.Consumed || state.Failures >= maxFailedAttempts)
                return Task.FromResult(false);
            if (state.Challenge.CodeHash != codeHash)
            {
                state.Failures++;
                return Task.FromResult(false);
            }
            state.Consumed = true;
            return Task.FromResult(true);
        }
        public Task InvalidateAsync(CorporateEmail email, EmailCodePurpose purpose,
            string codeHash, CancellationToken cancellationToken)
        {
            if (_challenges.TryGetValue(purpose, out var state) &&
                state.Challenge.CodeHash == codeHash)
            {
                state.Challenge = state.Challenge with { ExpiresAt = DateTimeOffset.UnixEpoch };
                LastChallenge = state.Challenge;
            }
            return Task.CompletedTask;
        }
        public Task SendCodeAsync(CorporateEmail email, EmailCodePurpose purpose, string code,
            CancellationToken cancellationToken)
        {
            if (FailDelivery) throw new InvalidOperationException("Email transport failed.");
            SentCode = code;
            return Task.CompletedTask;
        }
        public Task CreateAsync(AccountSession session, CancellationToken cancellationToken)
        {
            _sessions.Add(session.TokenHash, session);
            return Task.CompletedTask;
        }
        public Task<AccountSession?> FindActiveAsync(string tokenHash, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            var session = _sessions.GetValueOrDefault(tokenHash);
            return Task.FromResult(session is not null && session.ExpiresAt > now ? session : null);
        }
        public Task RevokeAsync(string tokenHash, CancellationToken cancellationToken)
        {
            _sessions.Remove(tokenHash);
            return Task.CompletedTask;
        }

        private sealed class ChallengeState(EmailCodeChallenge challenge, int failures)
        {
            public EmailCodeChallenge Challenge { get; set; } = challenge;
            public int Failures { get; set; } = failures;
            public bool Consumed { get; set; }
        }
    }
}
