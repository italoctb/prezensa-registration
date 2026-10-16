using Registration.Application.Abstractions.Integrations;
using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;
using Registration.Domain.Drafts;
using Registration.Domain.Registrations;
using RegistrationAggregate = Registration.Domain.Registrations.Registration;

namespace Registration.Application.Tests.Support;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class FakeDraftRepository : IRegistrationDraftRepository
{
    public RegistrationDraft? Draft { get; set; }
    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }
    public bool FailNextUpdate { get; set; }

    public Task<RegistrationDraft?> GetByIdAsync(RegistrationDraftId id, CancellationToken cancellationToken) =>
        Task.FromResult(Draft?.Id == id ? Draft : null);

    public Task<RegistrationDraft?> GetActiveByOwnerAsync(OwnerId ownerId, CancellationToken cancellationToken) =>
        Task.FromResult(Draft?.OwnerId == ownerId && Draft.Status == RegistrationDraftStatus.InProgress ? Draft : null);

    public Task<RegistrationDraft> AddOrGetActiveAsync(RegistrationDraft draft, CancellationToken cancellationToken)
    {
        if (Draft is { Status: RegistrationDraftStatus.InProgress } active && active.OwnerId == draft.OwnerId)
            return Task.FromResult(active);
        Draft = draft;
        AddCalls++;
        return Task.FromResult(draft);
    }

    public Task UpdateAsync(RegistrationDraft draft, long expectedVersion, CancellationToken cancellationToken)
    {
        if (FailNextUpdate)
        {
            FailNextUpdate = false;
            throw new IOException("Simulated Draft storage failure.");
        }
        if (draft.Version <= expectedVersion)
            throw new InvalidOperationException("Expected version was not advanced.");
        Draft = draft;
        UpdateCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeRegistrationRepository : IRegistrationRepository
{
    public bool ExistingCpf { get; set; }
    public Cpf? CheckedCpf { get; private set; }
    public RegistrationAggregate? Added { get; private set; }
    public int AddCalls { get; private set; }
    public Action<RegistrationAggregate>? OnAdd { get; set; }
    public RegistrationSubmission? ExistingDraftRegistration { get; set; }

    public Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        CheckedCpf = cpf;
        return Task.FromResult(ExistingCpf);
    }

    public Task<RegistrationSubmission?> FindByDraftIdAsync(RegistrationDraftId draftId, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingDraftRegistration);

    public Task<RegistrationId> AddForDraftAsync(RegistrationAggregate registration, RegistrationDraftId draftId,
        CancellationToken cancellationToken)
    {
        OnAdd?.Invoke(registration);
        Added = registration;
        AddCalls++;
        ExistingDraftRegistration = new RegistrationSubmission(registration.Id, registration.Applicant);
        return Task.FromResult(registration.Id);
    }
}

internal sealed class FakePostalCodeLookup : IPostalCodeLookup
{
    public PostalCode? Requested { get; private set; }
    public PostalCodeDetails? Response { get; set; }

    public Task<PostalCodeDetails?> LookupAsync(PostalCode postalCode, CancellationToken cancellationToken)
    {
        Requested = postalCode;
        return Task.FromResult(Response);
    }
}

internal static class Sample
{
    public static readonly DateTimeOffset T0 = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset T1 = T0.AddMinutes(1);
    public static readonly OwnerId Owner = OwnerId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    public static readonly OwnerId OtherOwner = OwnerId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    public static RegistrationDraft Draft() => RegistrationDraft.Start(RegistrationDraftId.From(Guid.NewGuid()), Owner, T0);

    public static void Complete(RegistrationDraft draft)
    {
        draft.SetFullName(FullName.Create("Ana Maria"), T1);
        draft.SetCpf(Cpf.Create("52998224725"), T1);
        draft.SetBirthDate(BirthDate.Create(new DateOnly(1990, 5, 20)), T1);
        draft.SetEmail(EmailAddress.Create("ana@example.com"), T1);
        draft.SetPhone(PhoneNumber.Create("11912345678"), T1);
        draft.SetMotherName(MotherName.Create("Maria Silva"), T1);
        draft.SetNationality(Nationality.Create("Brasileira"), T1);
        draft.SetAddress(Address.Create(PostalCode.Create("12345678"), Street.Create("Rua A"),
            District.Create("Centro"), City.Create("São Paulo"), FederativeUnit.Create("SP"),
            AddressNumber.Create("10")), T1);
        draft.SetProfession(Profession.Create("Engenheira"), T1);
        draft.SetMonthlyIncome(MonthlyIncome.Create(5000m), T1);
    }
}
