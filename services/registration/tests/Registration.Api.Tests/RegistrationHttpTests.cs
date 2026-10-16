using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Registration.Api.Contracts;
using Registration.Api.Tests.Support;
using Registration.Application;
using Registration.Domain;

namespace Registration.Api.Tests;

public sealed class RegistrationHttpTests
{
    [Fact]
    public async Task Unauthenticated_requests_are_rejected()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/drafts", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/postal-codes/01001000")).StatusCode);
    }

    [Fact]
    public async Task An_unrecognized_session_is_rejected()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "opaque-subject");

        using var response = await client.PostAsync("/drafts", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Start_reuses_active_draft_and_current_returns_its_snapshot()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.ClientFor(Guid.NewGuid());

        var first = await Start(client);
        var second = await Start(client);
        var current = await client.GetFromJsonAsync<DraftResponse>("/drafts/current");

        Assert.Equal(first.DraftId, second.DraftId);
        Assert.Equal(first.DraftId, current?.DraftId);
        Assert.Equal("InProgress", first.Status);
        Assert.Equal("FullName", first.CurrentStep);
        Assert.Null(first.Document);
        Assert.Equal(1, first.Version);
    }

    [Fact]
    public async Task Missing_current_draft_returns_a_stable_not_found_error()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.ClientFor(Guid.NewGuid());

        using var response = await client.GetAsync("/drafts/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.DraftNotFound, await ErrorCode(response));
    }

    [Fact]
    public async Task Another_owner_cannot_change_a_draft()
    {
        using var factory = new RegistrationApiFactory();
        using var owner = factory.ClientFor(Guid.NewGuid());
        using var other = factory.ClientFor(Guid.NewGuid());
        var draft = await Start(owner);

        using var response = await other.PutAsJsonAsync($"/drafts/{draft.DraftId}/full-name",
            new UpdateFullNameRequest("Ana Maria"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.DraftAccessDenied, await ErrorCode(response));
        Assert.Null((await owner.GetFromJsonAsync<DraftResponse>("/drafts/current"))?.FullName);
    }

    [Fact]
    public async Task Invalid_field_returns_domain_code_and_preserves_draft()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.ClientFor(Guid.NewGuid());
        var draft = await Start(client);

        using var response = await client.PutAsJsonAsync($"/drafts/{draft.DraftId}/full-name",
            new UpdateFullNameRequest("Al"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(DomainErrorCodes.InvalidName, await ErrorCode(response));
        Assert.Equal(1, (await client.GetFromJsonAsync<DraftResponse>("/drafts/current"))?.Version);
    }

    [Fact]
    public async Task Omitted_monthly_income_is_not_interpreted_as_zero()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.ClientFor(Guid.NewGuid());
        var draft = await Start(client);

        using var response = await client.PutAsJsonAsync($"/drafts/{draft.DraftId}/monthly-income", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(DomainErrorCodes.InvalidMonthlyIncome, await ErrorCode(response));
    }

    [Fact]
    public async Task Postal_code_lookup_returns_local_fields_or_not_found()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.ClientFor(Guid.NewGuid());

        var found = await client.GetFromJsonAsync<PostalCodeResponse>("/postal-codes/01001000");
        using var missing = await client.GetAsync("/postal-codes/99999999");
        using var malformed = await client.GetAsync("/postal-codes/bad");

        Assert.Equal("São Paulo", found?.City);
        Assert.Equal("SP", found?.FederativeUnit);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(ApplicationErrorCodes.PostalCodeNotFound, await ErrorCode(missing));
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Equal(DomainErrorCodes.InvalidAddress, await ErrorCode(malformed));
    }

    [Fact]
    public async Task Onboarding_updates_each_field_and_submit_creates_registration()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.ClientFor(Guid.NewGuid());
        var draft = await Start(client);
        var route = $"/drafts/{draft.DraftId}";
        var review = await CompleteDraft(client, route);

        Assert.Equal("Review", review.CurrentStep);
        Assert.Equal("52998224725", review.Document);
        Assert.Equal("01001000", review.Address?.PostalCode);
        Assert.Equal(11, review.Version);

        using var submitted = await client.PostAsync($"{route}/submit", null);
        var result = await submitted.Content.ReadFromJsonAsync<SubmitDraftResponse>();
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        Assert.NotEqual(Guid.Empty, result?.RegistrationId);
        Assert.Equal(1, factory.Services.GetRequiredService<FakeRegistrationStore>().RegistrationsAdded);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/drafts/current")).StatusCode);
    }

    [Fact]
    public async Task Duplicate_cpf_is_reported_as_conflict()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.ClientFor(Guid.NewGuid());
        var draft = await Start(client);
        await CompleteDraft(client, $"/drafts/{draft.DraftId}");
        factory.Services.GetRequiredService<FakeRegistrationStore>().CpfAlreadyRegistered = true;

        using var response = await client.PostAsync($"/drafts/{draft.DraftId}/submit", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.CpfAlreadyRegistered, await ErrorCode(response));
    }

    [Fact]
    public async Task Another_owner_cannot_submit_a_draft()
    {
        using var factory = new RegistrationApiFactory();
        using var owner = factory.ClientFor(Guid.NewGuid());
        using var other = factory.ClientFor(Guid.NewGuid());
        var draft = await Start(owner);

        using var response = await other.PostAsync($"/drafts/{draft.DraftId}/submit", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.DraftAccessDenied, await ErrorCode(response));
    }

    [Fact]
    public async Task Incomplete_submit_returns_domain_error()
    {
        using var factory = new RegistrationApiFactory();
        using var client = factory.ClientFor(Guid.NewGuid());
        var draft = await Start(client);

        using var response = await client.PostAsync($"/drafts/{draft.DraftId}/submit", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(DomainErrorCodes.IncompleteDraft, await ErrorCode(response));
    }

    private static async Task<DraftResponse> Start(HttpClient client)
    {
        using var response = await client.PostAsync("/drafts", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DraftResponse>())!;
    }

    private static async Task<DraftResponse> CompleteDraft(HttpClient client, string route)
    {
        await PutOk(client, $"{route}/full-name", new UpdateFullNameRequest("Ana Maria"));
        await PutOk(client, $"{route}/document", new UpdateDocumentRequest("529.982.247-25"));
        await PutOk(client, $"{route}/birth-date", new UpdateBirthDateRequest(new DateOnly(1990, 5, 20)));
        await PutOk(client, $"{route}/email", new UpdateEmailRequest("ana@example.com"));
        await PutOk(client, $"{route}/phone", new UpdatePhoneRequest("11912345678"));
        await PutOk(client, $"{route}/mother-name", new UpdateMotherNameRequest("Maria Silva"));
        await PutOk(client, $"{route}/nationality", new UpdateNationalityRequest("Brasileira"));
        await PutOk(client, $"{route}/address", new UpdateAddressRequest("01001-000", "Praça da Sé",
            "Sé", "São Paulo", "SP", "10", null));
        await PutOk(client, $"{route}/profession", new UpdateProfessionRequest("Engenheira"));
        return await PutOk(client, $"{route}/monthly-income", new UpdateMonthlyIncomeRequest(5000m));
    }

    private static async Task<DraftResponse> PutOk<T>(HttpClient client, string route, T request)
    {
        using var response = await client.PutAsJsonAsync(route, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DraftResponse>())!;
    }

    private static async Task<string?> ErrorCode(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("code").GetString();
    }
}
