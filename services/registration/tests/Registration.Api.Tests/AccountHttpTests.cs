using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Registration.Api.Contracts;
using Registration.Api.Tests.Support;
using Registration.Application.Accounts;

namespace Registration.Api.Tests;

public sealed class AccountHttpTests
{
    [Fact]
    public async Task Verified_account_can_log_in_and_use_a_revocable_session()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.CreateClient();
        var store = factory.Services.GetRequiredService<FakeAccountAccessStore>();

        using var requested = await client.PostAsJsonAsync("/accounts/codes",
            new EmailRequest("  Ana.Silva@PREZENSA.COM "));
        Assert.Equal(HttpStatusCode.Accepted, requested.StatusCode);
        Assert.DoesNotContain("code", await requested.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var signupCode = store.SentCode("ana.silva@prezensa.com", EmailCodePurpose.CreateAccount);
        Assert.Matches("^[0-9]{8}$", signupCode!);

        using var created = await client.PostAsJsonAsync("/accounts",
            new ConfirmEmailCodeRequest("ana.silva@prezensa.com", signupCode));
        var session = await created.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotEqual(Guid.Empty, session?.OwnerId);
        Assert.Equal("Bearer", session?.TokenType);
        Assert.Equal(43, session?.AccessToken.Length);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/drafts", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/sessions/current")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/drafts", null)).StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/sessions/codes",
            new EmailRequest("ana.silva@prezensa.com"))).StatusCode);
        var loginCode = store.SentCode("ana.silva@prezensa.com", EmailCodePurpose.Login);
        using var loggedIn = await client.PostAsJsonAsync("/sessions",
            new ConfirmEmailCodeRequest("ana.silva@prezensa.com", loginCode));
        var second = await loggedIn.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
        Assert.Equal(session.OwnerId, second?.OwnerId);
        Assert.NotEqual(session.AccessToken, second?.AccessToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", second!.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/drafts/current")).StatusCode);
    }

    [Fact]
    public async Task Codes_are_single_use_and_purpose_bound()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.CreateClient();
        var store = factory.Services.GetRequiredService<FakeAccountAccessStore>();
        await client.PostAsJsonAsync("/accounts/codes", new EmailRequest("ana@prezensa.com"));
        var code = store.SentCode("ana@prezensa.com", EmailCodePurpose.CreateAccount);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/sessions",
            new ConfirmEmailCodeRequest("ana@prezensa.com", code))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/accounts",
            new ConfirmEmailCodeRequest("ana@prezensa.com", code))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/accounts",
            new ConfirmEmailCodeRequest("ana@prezensa.com", code))).StatusCode);
    }

    [Fact]
    public async Task Unknown_login_and_existing_signup_have_the_same_accepted_response()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.CreateClient();
        var store = factory.Services.GetRequiredService<FakeAccountAccessStore>();

        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/sessions/codes",
            new EmailRequest("nobody@prezensa.com"))).StatusCode);
        Assert.Null(store.SentCode("nobody@prezensa.com", EmailCodePurpose.Login));

        await client.PostAsJsonAsync("/accounts/codes", new EmailRequest("ana@prezensa.com"));
        var code = store.SentCode("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        await client.PostAsJsonAsync("/accounts", new ConfirmEmailCodeRequest("ana@prezensa.com", code));
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/accounts/codes",
            new EmailRequest("ana@prezensa.com"))).StatusCode);
    }

    [Theory]
    [InlineData("ana@example.com")]
    [InlineData("ana@sub.prezensa.com")]
    [InlineData("ana@prezensa.com.evil.test")]
    public async Task Non_corporate_email_is_rejected(string email)
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/accounts/codes",
            new EmailRequest(email))).StatusCode);
    }

    [Fact]
    public async Task Expired_code_and_session_are_rejected()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.CreateClient();
        var store = factory.Services.GetRequiredService<FakeAccountAccessStore>();
        await client.PostAsJsonAsync("/accounts/codes", new EmailRequest("ana@prezensa.com"));
        var code = store.SentCode("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        factory.Advance(TimeSpan.FromMinutes(10));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/accounts",
            new ConfirmEmailCodeRequest("ana@prezensa.com", code))).StatusCode);

        factory.Advance(TimeSpan.FromMinutes(1));
        await client.PostAsJsonAsync("/accounts/codes", new EmailRequest("ana@prezensa.com"));
        code = store.SentCode("ana@prezensa.com", EmailCodePurpose.CreateAccount);
        var created = await client.PostAsJsonAsync("/accounts",
            new ConfirmEmailCodeRequest("ana@prezensa.com", code));
        var session = await created.Content.ReadFromJsonAsync<SessionResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        factory.Advance(TimeSpan.FromHours(12));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/drafts", null)).StatusCode);
    }
}
