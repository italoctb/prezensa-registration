using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Registration.Api.Startup;
using Registration.Application.Abstractions.Integrations;
using Registration.Application.Abstractions.Persistence;
using Registration.Application.Accounts;
using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;
using Registration.Domain.Drafts;
using Registration.Domain.Registrations;

namespace Registration.Api.Tests.Support;

public sealed class RegistrationApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Registration:Authentication:CodeHashKey"] = Convert.ToBase64String(new byte[32]),
                ["Registration:Authentication:Smtp:Host"] = "smtp.example.test",
                ["Registration:Authentication:Smtp:Port"] = "587",
                ["Registration:Authentication:Smtp:From"] = "no-reply@prezensa.com",
                ["Registration:Authentication:Smtp:Security"] = "StartTls",
                ["Registration:Mongo:ConnectionString"] = "mongodb://localhost:27017",
                ["Registration:Mongo:Database"] = "registration_test",
                ["Registration:Oracle:ConnectionString"] = "User Id=test;Password=test;Data Source=localhost/FREEPDB1"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRegistrationDraftRepository>();
            services.RemoveAll<IRegistrationRepository>();
            services.RemoveAll<IPostalCodeLookup>();
            services.RemoveAll<IAccountRepository>();
            services.RemoveAll<IEmailCodeRepository>();
            services.RemoveAll<IAccountSessionRepository>();
            services.RemoveAll<IAuthenticationEmailSender>();
            var schemaStartup = services.Single(descriptor => descriptor.ImplementationType == typeof(OracleSchemaStartup));
            services.Remove(schemaStartup);

            services.AddSingleton<FakeRegistrationStore>();
            services.AddSingleton<IRegistrationDraftRepository>(sp => sp.GetRequiredService<FakeRegistrationStore>());
            services.AddSingleton<IRegistrationRepository>(sp => sp.GetRequiredService<FakeRegistrationStore>());
            services.AddSingleton<IPostalCodeLookup, FakePostalCodeLookup>();
            services.AddSingleton<FakeAccountAccessStore>();
            services.AddSingleton<IAccountRepository>(sp => sp.GetRequiredService<FakeAccountAccessStore>());
            services.AddSingleton<IEmailCodeRepository>(sp => sp.GetRequiredService<FakeAccountAccessStore>());
            services.AddSingleton<IAccountSessionRepository>(sp => sp.GetRequiredService<FakeAccountAccessStore>());
            services.AddSingleton<IAuthenticationEmailSender>(sp => sp.GetRequiredService<FakeAccountAccessStore>());
            services.AddSingleton<FixedTimeProvider>(new FixedTimeProvider(Now));
            services.AddSingleton<TimeProvider>(sp => sp.GetRequiredService<FixedTimeProvider>());
        });
    }

    public HttpClient ClientFor(Guid owner)
    {
        var client = CreateClient();
        var token = Services.GetRequiredService<FakeAccountAccessStore>().SeedSession(
            OwnerId.From(owner), Now.AddHours(12));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public void Advance(TimeSpan duration) => Services.GetRequiredService<FixedTimeProvider>().Advance(duration);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan duration) => now = now.Add(duration);
}

public sealed class FakeRegistrationStore : IRegistrationDraftRepository, IRegistrationRepository
{
    private readonly Dictionary<RegistrationDraftId, RegistrationDraft> _drafts = [];
    private readonly Dictionary<RegistrationDraftId, RegistrationSubmission> _registrations = [];
    public bool CpfAlreadyRegistered { get; set; }
    public int RegistrationsAdded { get; private set; }

    public Task<RegistrationDraft?> GetByIdAsync(RegistrationDraftId id, CancellationToken cancellationToken) =>
        Task.FromResult(_drafts.GetValueOrDefault(id));

    public Task<RegistrationDraft?> GetActiveByOwnerAsync(OwnerId ownerId, CancellationToken cancellationToken) =>
        Task.FromResult(_drafts.Values.FirstOrDefault(draft =>
            draft.OwnerId == ownerId && draft.Status == RegistrationDraftStatus.InProgress));

    public Task<RegistrationDraft> AddOrGetActiveAsync(RegistrationDraft draft, CancellationToken cancellationToken)
    {
        var existing = _drafts.Values.FirstOrDefault(value =>
            value.OwnerId == draft.OwnerId && value.Status == RegistrationDraftStatus.InProgress);
        if (existing is not null) return Task.FromResult(existing);
        _drafts.Add(draft.Id, draft);
        return Task.FromResult(draft);
    }

    public Task UpdateAsync(RegistrationDraft draft, long expectedVersion, CancellationToken cancellationToken)
    {
        if (draft.Version <= expectedVersion) throw new InvalidOperationException("Version did not advance.");
        _drafts[draft.Id] = draft;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken) =>
        Task.FromResult(CpfAlreadyRegistered || _registrations.Values.Any(value => value.Applicant.Cpf == cpf));

    public Task<RegistrationSubmission?> FindByDraftIdAsync(RegistrationDraftId draftId,
        CancellationToken cancellationToken) => Task.FromResult(_registrations.GetValueOrDefault(draftId));

    public Task<RegistrationId> AddForDraftAsync(Registration.Domain.Registrations.Registration registration,
        RegistrationDraftId draftId, CancellationToken cancellationToken)
    {
        _registrations.Add(draftId, new(registration.Id, registration.Applicant));
        RegistrationsAdded++;
        return Task.FromResult(registration.Id);
    }
}

internal sealed class FakePostalCodeLookup : IPostalCodeLookup
{
    public Task<PostalCodeDetails?> LookupAsync(PostalCode postalCode, CancellationToken cancellationToken) =>
        Task.FromResult(postalCode.Digits == "01001000"
            ? new PostalCodeDetails(postalCode, Street.Create("Praça da Sé"), District.Create("Sé"),
                City.Create("São Paulo"), FederativeUnit.Create("SP"))
            : null);
}
