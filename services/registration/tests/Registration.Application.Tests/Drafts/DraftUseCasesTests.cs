using Registration.Application;
using Registration.Application.Drafts;
using Registration.Application.Drafts.GetCurrent;
using Registration.Application.Drafts.LookupPostalCode;
using Registration.Application.Drafts.Start;
using Registration.Application.Drafts.UpdateAddress;
using Registration.Application.Drafts.UpdateBirthDate;
using Registration.Application.Drafts.UpdateDocument;
using Registration.Application.Drafts.UpdateEmail;
using Registration.Application.Drafts.UpdateFullName;
using Registration.Application.Drafts.UpdateMonthlyIncome;
using Registration.Application.Drafts.UpdateMotherName;
using Registration.Application.Drafts.UpdateNationality;
using Registration.Application.Drafts.UpdatePhone;
using Registration.Application.Drafts.UpdateProfession;
using Registration.Application.Tests.Support;
using Registration.Domain;
using Registration.Domain.Applicants.Addresses;
using Registration.Domain.Drafts;

namespace Registration.Application.Tests.Drafts;

public sealed class DraftUseCasesTests
{
    [Fact]
    public async Task Start_creates_once_and_reuses_active_draft()
    {
        var repository = new FakeDraftRepository();
        var handler = new StartDraftHandler(repository, new FixedTimeProvider(Sample.T1));
        var first = await handler.HandleAsync(new StartDraftCommand(Sample.Owner));
        var second = await handler.HandleAsync(new StartDraftCommand(Sample.Owner));
        Assert.Equal(first.DraftId, second.DraftId);
        Assert.Equal(Sample.Owner, first.OwnerId);
        Assert.Equal(RegistrationDraftStep.FullName, first.CurrentStep);
        Assert.Equal(Sample.T1, first.CreatedAt);
        Assert.Equal(1, repository.AddCalls);
    }

    [Fact]
    public async Task GetCurrent_returns_snapshot_or_null()
    {
        var repository = new FakeDraftRepository();
        var handler = new GetCurrentDraftHandler(repository);
        Assert.Null(await handler.HandleAsync(new GetCurrentDraftQuery(Sample.Owner)));
        var draft = Sample.Draft();
        draft.SetEmail(Registration.Domain.Applicants.EmailAddress.Create("ana@example.com"), Sample.T0);
        repository.Draft = draft;
        var snapshot = await handler.HandleAsync(new GetCurrentDraftQuery(Sample.Owner));
        Assert.NotNull(snapshot);
        Assert.Equal(draft.Id, snapshot.DraftId);
        Assert.Equal(draft.Email, snapshot.Email);
        Assert.Equal(draft.CurrentStep, snapshot.CurrentStep);
        Assert.Equal(draft.Version, snapshot.Version);
        Assert.Null(await handler.HandleAsync(new GetCurrentDraftQuery(Sample.OtherOwner)));
    }

    [Theory]
    [InlineData("fullName")]
    [InlineData("document")]
    [InlineData("birthDate")]
    [InlineData("email")]
    [InlineData("phone")]
    [InlineData("motherName")]
    [InlineData("nationality")]
    [InlineData("address")]
    [InlineData("profession")]
    [InlineData("monthlyIncome")]
    public async Task Each_update_persists_valid_value_and_enforces_access_state_and_value_rules(string field)
    {
        var repository = new FakeDraftRepository();
        var draft = Sample.Draft();
        repository.Draft = draft;
        var before = draft.Version;

        var result = await Apply(field, repository, draft.Id, Sample.Owner, false);
        Assert.Equal(before + 1, result.Version);
        Assert.Equal(Sample.T1, result.UpdatedAt);
        Assert.Equal(1, repository.UpdateCalls);
        Assert.Equal(result.Version, repository.Draft?.Version);

        await Assert.ThrowsAsync<DomainException>(() => Apply(field, repository, draft.Id, Sample.Owner, true));
        Assert.Equal(1, repository.UpdateCalls);
        Assert.Equal(before + 1, draft.Version);

        var denied = await Assert.ThrowsAsync<RegistrationApplicationException>(() =>
            Apply(field, repository, draft.Id, Sample.OtherOwner, false));
        Assert.Equal(ApplicationErrorCodes.DraftAccessDenied, denied.Code);
        Assert.Equal(1, repository.UpdateCalls);

        var missing = await Assert.ThrowsAsync<RegistrationApplicationException>(() =>
            Apply(field, repository, RegistrationDraftId.From(Guid.NewGuid()), Sample.Owner, false));
        Assert.Equal(ApplicationErrorCodes.DraftNotFound, missing.Code);
        Assert.Equal(1, repository.UpdateCalls);

        Sample.Complete(draft);
        draft.MarkSubmitted(Registration.Domain.Registrations.RegistrationId.From(Guid.NewGuid()), Sample.T1);
        await Assert.ThrowsAsync<DomainException>(() => Apply(field, repository, draft.Id, Sample.Owner, false));
        Assert.Equal(1, repository.UpdateCalls);
    }

    [Fact]
    public async Task Postal_lookup_passes_normalized_cep_and_reports_absence()
    {
        var port = new FakePostalCodeLookup();
        var handler = new LookupPostalCodeHandler(port);
        port.Response = new(PostalCode.Create("12345678"), Street.Create("Rua A"), District.Create("Centro"),
            City.Create("São Paulo"), FederativeUnit.Create("SP"));
        var result = await handler.HandleAsync(new LookupPostalCodeQuery("12345-678"));
        Assert.Equal("12345678", port.Requested?.Digits);
        Assert.Equal("SP", result.FederativeUnit.Code);
        port.Response = null;
        var notFound = await Assert.ThrowsAsync<RegistrationApplicationException>(() =>
            handler.HandleAsync(new LookupPostalCodeQuery("12345678")));
        Assert.Equal(ApplicationErrorCodes.PostalCodeNotFound, notFound.Code);
        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new LookupPostalCodeQuery("bad")));
    }

    private static Task<DraftSnapshot> Apply(string field, FakeDraftRepository repository,
        RegistrationDraftId id, OwnerId owner, bool invalid)
    {
        var time = new FixedTimeProvider(Sample.T1);
        return field switch
        {
            "fullName" => new UpdateFullNameHandler(repository, time).HandleAsync(new(id, owner, invalid ? "x" : " Ana Maria ")),
            "document" => new UpdateDocumentHandler(repository, time).HandleAsync(new(id, owner, invalid ? "123" : "529.982.247-25")),
            "birthDate" => new UpdateBirthDateHandler(repository, time).HandleAsync(new(id, owner, invalid ? default : new DateOnly(1990, 5, 20))),
            "email" => new UpdateEmailHandler(repository, time).HandleAsync(new(id, owner, invalid ? "bad" : "ana@example.com")),
            "phone" => new UpdatePhoneHandler(repository, time).HandleAsync(new(id, owner, invalid ? "123" : "(11) 91234-5678")),
            "motherName" => new UpdateMotherNameHandler(repository, time).HandleAsync(new(id, owner, invalid ? " " : "Maria Silva")),
            "nationality" => new UpdateNationalityHandler(repository, time).HandleAsync(new(id, owner, invalid ? " " : "Brasileira")),
            "address" => new UpdateAddressHandler(repository, time).HandleAsync(new(id, owner,
                invalid ? "123" : "12345-678", "Rua A", "Centro", "São Paulo", "SP", "10", null)),
            "profession" => new UpdateProfessionHandler(repository, time).HandleAsync(new(id, owner, invalid ? " " : "Engenheira")),
            "monthlyIncome" => new UpdateMonthlyIncomeHandler(repository, time).HandleAsync(new(id, owner, invalid ? -1m : 5000m)),
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };
    }
}
