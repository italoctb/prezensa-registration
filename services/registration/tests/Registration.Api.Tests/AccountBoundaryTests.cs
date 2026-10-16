using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Registration.Api.Security;
using Registration.Api.Tests.Support;
using Registration.Application.Abstractions.Persistence;
using Registration.Application.Accounts.Sessions;
using Registration.Domain.Drafts;

namespace Registration.Api.Tests;

public sealed class AccountBoundaryTests
{
    [Fact]
    public async Task Opaque_bearer_scheme_derives_owner_from_active_session()
    {
        var now = RegistrationApiFactory.Now;
        var owner = OwnerId.From(Guid.NewGuid());
        var store = new FakeAccountAccessStore();
        var token = store.SeedSession(owner, now.AddHours(12));
        using var services = BuildServices(store, now);
        using var scope = services.CreateScope();
        var context = Context(scope.ServiceProvider, $"Bearer {token}");

        var result = await context.AuthenticateAsync("Bearer");

        Assert.True(result.Succeeded);
        Assert.Equal(owner.Value.ToString("D"), result.Principal?.FindFirstValue("sub"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Basic abc")]
    [InlineData("Bearer malformed")]
    public async Task Absent_or_invalid_session_cannot_authenticate(string? header)
    {
        using var services = BuildServices(new FakeAccountAccessStore(), RegistrationApiFactory.Now);
        using var scope = services.CreateScope();
        var result = await Context(scope.ServiceProvider, header).AuthenticateAsync("Bearer");
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Revoked_and_expired_tokens_cannot_authenticate()
    {
        var now = RegistrationApiFactory.Now;
        var store = new FakeAccountAccessStore();
        var owner = OwnerId.From(Guid.NewGuid());
        var expired = store.SeedSession(owner, now);
        var revoked = store.SeedSession(owner, now.AddHours(1));
        await store.RevokeAsync(Registration.Application.Accounts.AuthenticationPolicy.HashSessionToken(revoked),
            CancellationToken.None);
        using var services = BuildServices(store, now);
        using var scope = services.CreateScope();

        Assert.False((await Context(scope.ServiceProvider, $"Bearer {expired}")
            .AuthenticateAsync("Bearer")).Succeeded);
        Assert.False((await Context(scope.ServiceProvider, $"Bearer {revoked}")
            .AuthenticateAsync("Bearer")).Succeeded);
    }

    private static ServiceProvider BuildServices(FakeAccountAccessStore store, DateTimeOffset now)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(now));
        services.AddSingleton<IAccountSessionRepository>(store);
        services.AddScoped<SessionAccess>();
        services.AddAuthentication("Bearer")
            .AddScheme<AuthenticationSchemeOptions, OpaqueBearerHandler>("Bearer", _ => { });
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext Context(IServiceProvider services, string? authorization)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        if (authorization is not null) context.Request.Headers.Authorization = authorization;
        return context;
    }
}
