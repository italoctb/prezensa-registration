using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Driver;
using Registration.Application.Accounts;
using Registration.Domain.Drafts;
using Registration.Infrastructure.Persistence.Mongo;

namespace Registration.Infrastructure.Tests.Persistence;

public sealed class MongoAccountAccessRepositoryTests : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder("mongo:8.0")
        .WithPortBinding(27017, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(27017))
        .Build();
    private MongoAccountRepository _accounts = null!;
    private MongoEmailCodeRepository _codes = null!;
    private MongoAccountSessionRepository _sessions = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var connection = $"mongodb://{_container.Hostname}:{_container.GetMappedPublicPort(27017)}";
        var database = new MongoClient(connection).GetDatabase($"registration_auth_tests_{Guid.NewGuid():N}");
        _accounts = new MongoAccountRepository(database);
        _codes = new MongoEmailCodeRepository(database);
        _sessions = new MongoAccountSessionRepository(database);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Accounts_are_unique_by_canonical_email_and_keep_stable_owner()
    {
        var email = CorporateEmail.Create("ANA@PREZENSA.COM");
        var owner = OwnerId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        Assert.True(await _accounts.TryCreateAsync(new UserAccount(owner, email, now), CancellationToken.None));
        Assert.False(await _accounts.TryCreateAsync(new UserAccount(
            OwnerId.From(Guid.NewGuid()), email, now), CancellationToken.None));
        Assert.Equal(owner, (await _accounts.FindByEmailAsync(
            CorporateEmail.Create("ana@prezensa.com"), CancellationToken.None))?.Id);
    }

    [Fact]
    public async Task Code_is_cooldown_limited_single_use_and_purpose_bound()
    {
        var email = CorporateEmail.Create("ana@prezensa.com");
        var now = DateTimeOffset.UtcNow;
        var code = new EmailCodeChallenge(email, EmailCodePurpose.CreateAccount, "hash-1", now,
            now.AddMinutes(10));
        Assert.True(await _codes.TryIssueAsync(code, TimeSpan.FromMinutes(1), 5, CancellationToken.None));
        Assert.False(await _codes.TryIssueAsync(code with { CodeHash = "hash-2" },
            TimeSpan.FromMinutes(1), 5, CancellationToken.None));
        Assert.False(await _codes.TryConsumeAsync(email, EmailCodePurpose.Login, "hash-1", now,
            5, CancellationToken.None));

        var attempts = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            _codes.TryConsumeAsync(email, EmailCodePurpose.CreateAccount, "hash-1", now,
                5, CancellationToken.None)));
        Assert.Single(attempts, value => value);
        Assert.False(await _codes.TryConsumeAsync(email, EmailCodePurpose.CreateAccount, "hash-1", now,
            5, CancellationToken.None));
    }

    [Fact]
    public async Task Failures_survive_resend_and_lock_until_expiry()
    {
        var email = CorporateEmail.Create("ana@prezensa.com");
        var now = DateTimeOffset.UtcNow;
        var first = new EmailCodeChallenge(email, EmailCodePurpose.Login, "hash-1", now,
            now.AddMinutes(10));
        Assert.True(await _codes.TryIssueAsync(first, TimeSpan.FromMinutes(1), 5, CancellationToken.None));
        for (var i = 0; i < 4; i++)
            Assert.False(await _codes.TryConsumeAsync(email, EmailCodePurpose.Login, "wrong", now,
                5, CancellationToken.None));
        var second = first with { CodeHash = "hash-2", IssuedAt = now.AddMinutes(1),
            ExpiresAt = now.AddMinutes(11) };
        Assert.True(await _codes.TryIssueAsync(second, TimeSpan.FromMinutes(1), 5, CancellationToken.None));
        Assert.False(await _codes.TryConsumeAsync(email, EmailCodePurpose.Login, "wrong",
            now.AddMinutes(1), 5, CancellationToken.None));
        Assert.False(await _codes.TryConsumeAsync(email, EmailCodePurpose.Login, "hash-2",
            now.AddMinutes(1), 5, CancellationToken.None));
        Assert.False(await _codes.TryIssueAsync(second with { CodeHash = "hash-3",
            IssuedAt = now.AddMinutes(2) }, TimeSpan.FromMinutes(1), 5, CancellationToken.None));
    }

    [Fact]
    public async Task Session_can_be_revoked_and_expiry_is_checked_without_waiting_for_ttl()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var session = new AccountSession("token-hash", owner, now, now.AddHours(1));
        await _sessions.CreateAsync(session, CancellationToken.None);
        Assert.Equal(owner, (await _sessions.FindActiveAsync("token-hash", now,
            CancellationToken.None))?.OwnerId);
        Assert.Null(await _sessions.FindActiveAsync("token-hash", now.AddHours(1),
            CancellationToken.None));
        await _sessions.RevokeAsync("token-hash", CancellationToken.None);
        Assert.Null(await _sessions.FindActiveAsync("token-hash", now, CancellationToken.None));
    }
}
