using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;
using Registration.Domain.Drafts;
using Registration.Domain.Drafts.Events;
using Registration.Domain.Registrations;
using Registration.Domain.Tests.Registrations;

namespace Registration.Domain.Tests.Drafts;

public sealed class RegistrationDraftTests
{
    private static readonly DateTimeOffset T0 = RegistrationSample.T0;
    private static readonly OwnerId Owner = OwnerId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    [Fact]
    public void Start_sets_identity_owner_empty_fields_first_step_and_event()
    {
        var draft = New();
        Assert.Equal(Owner, draft.OwnerId);
        Assert.Equal(RegistrationDraftStatus.InProgress, draft.Status);
        Assert.Equal(RegistrationDraftStep.FullName, draft.CurrentStep);
        Assert.Null(draft.FullName);
        Assert.Null(draft.Cpf);
        Assert.Null(draft.BirthDate);
        Assert.Null(draft.Email);
        Assert.Null(draft.Phone);
        Assert.Null(draft.MotherName);
        Assert.Null(draft.Nationality);
        Assert.Null(draft.Address);
        Assert.Null(draft.Profession);
        Assert.Null(draft.MonthlyIncome);
        Assert.Null(draft.SubmittedAt);
        Assert.Null(draft.RegistrationId);
        Assert.Equal(T0, draft.CreatedAt);
        Assert.Equal(T0, draft.UpdatedAt);
        Assert.Equal(1L, draft.Version);
        Assert.Equal(Owner, Assert.IsType<RegistrationDraftStarted>(Assert.Single(draft.DomainEvents)).OwnerId);
        Assert.False(draft.CanSubmit(DateOnly.FromDateTime(T0.UtcDateTime)));
    }

    [Fact]
    public void Start_rejects_empty_ids_and_non_utc_time()
    {
        Assert.Equal(DomainErrorCodes.InvalidId,
            Assert.Throws<DomainException>(() => RegistrationDraft.Start(default, Owner, T0)).Code);
        Assert.Equal(DomainErrorCodes.InvalidId,
            Assert.Throws<DomainException>(() => RegistrationDraft.Start(RegistrationDraftId.From(Guid.NewGuid()), default, T0)).Code);
        Assert.Equal(DomainErrorCodes.InvalidTimestamp,
            Assert.Throws<DomainException>(() => RegistrationDraft.Start(RegistrationDraftId.From(Guid.NewGuid()), Owner,
                new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.FromHours(-3)))).Code);
    }

    [Fact]
    public void Every_field_advances_version_time_and_step()
    {
        var draft = New();
        var profile = RegistrationSample.Applicant();
        draft.SetFullName(profile.FullName, T0.AddMinutes(1));
        Assert.Equal(RegistrationDraftStep.Document, draft.CurrentStep);
        draft.SetCpf(profile.Cpf, T0.AddMinutes(2));
        Assert.Equal(RegistrationDraftStep.BirthDate, draft.CurrentStep);
        draft.SetBirthDate(profile.BirthDate, T0.AddMinutes(3));
        Assert.Equal(RegistrationDraftStep.Email, draft.CurrentStep);
        draft.SetEmail(profile.Email, T0.AddMinutes(4));
        Assert.Equal(RegistrationDraftStep.Phone, draft.CurrentStep);
        draft.SetPhone(profile.Phone, T0.AddMinutes(5));
        Assert.Equal(RegistrationDraftStep.MotherName, draft.CurrentStep);
        draft.SetMotherName(profile.MotherName, T0.AddMinutes(6));
        Assert.Equal(RegistrationDraftStep.Nationality, draft.CurrentStep);
        draft.SetNationality(profile.Nationality, T0.AddMinutes(7));
        Assert.Equal(RegistrationDraftStep.Address, draft.CurrentStep);
        draft.SetAddress(profile.Address, T0.AddMinutes(8));
        Assert.Equal(RegistrationDraftStep.Profession, draft.CurrentStep);
        draft.SetProfession(profile.Profession, T0.AddMinutes(9));
        Assert.Equal(RegistrationDraftStep.MonthlyIncome, draft.CurrentStep);
        draft.SetMonthlyIncome(profile.MonthlyIncome, T0.AddMinutes(10));

        Assert.Equal(RegistrationDraftStep.Review, draft.CurrentStep);
        Assert.True(draft.CanSubmit(DateOnly.FromDateTime(T0.UtcDateTime)));
        Assert.Equal(profile, draft.ToApplicantProfile(DateOnly.FromDateTime(T0.UtcDateTime)));
        Assert.Equal(11L, draft.Version);
        Assert.Equal(T0.AddMinutes(10), draft.UpdatedAt);
        Assert.Single(draft.DomainEvents);
    }

    [Fact]
    public void Missing_data_and_underage_block_submit_even_if_other_data_is_present()
    {
        var draft = New();
        Assert.Equal(DomainErrorCodes.IncompleteDraft,
            Assert.Throws<DomainException>(() => draft.EnsureCanSubmit(DateOnly.FromDateTime(T0.UtcDateTime))).Code);
        Assert.Equal(DomainErrorCodes.IncompleteDraft,
            Assert.Throws<DomainException>(() => draft.MarkSubmitted(RegistrationId.From(Guid.NewGuid()), T0.AddMinutes(1))).Code);
        Assert.Equal(RegistrationDraftStatus.InProgress, draft.Status);
        Fill(draft, RegistrationSample.Applicant(new DateOnly(2008, 10, 13)));
        Assert.Equal(RegistrationDraftStep.Review, draft.CurrentStep);
        Assert.False(draft.CanSubmit(DateOnly.FromDateTime(T0.UtcDateTime)));
        Assert.Equal(DomainErrorCodes.UnderageApplicant,
            Assert.Throws<DomainException>(() => draft.ToApplicantProfile(DateOnly.FromDateTime(T0.UtcDateTime))).Code);
        Assert.Equal(DomainErrorCodes.InvalidBirthDate,
            Assert.Throws<DomainException>(() => draft.EnsureCanSubmit(new DateOnly(2008, 1, 1))).Code);
        draft.SetBirthDate(BirthDate.Create(new DateOnly(2008, 10, 12)), T0.AddMinutes(11));
        Assert.True(draft.CanSubmit(DateOnly.FromDateTime(T0.UtcDateTime)));
        Assert.Equal(DomainErrorCodes.InvalidTimestamp,
            Assert.Throws<DomainException>(() => draft.EnsureCanSubmitAt(T0)).Code);
    }

    [Fact]
    public void Submission_is_terminal_and_associates_registration()
    {
        var draft = New();
        Fill(draft, RegistrationSample.Applicant());
        var id = RegistrationId.From(Guid.NewGuid());
        var at = T0.AddMinutes(11);
        draft.MarkSubmitted(id, at);

        Assert.Equal(RegistrationDraftStatus.Submitted, draft.Status);
        Assert.Equal(id, draft.RegistrationId);
        Assert.Equal(at, draft.SubmittedAt);
        Assert.Equal(at, draft.UpdatedAt);
        Assert.Equal(12L, draft.Version);
        Assert.False(draft.CanSubmit(DateOnly.FromDateTime(at.UtcDateTime)));
        Assert.Equal(id, Assert.IsType<RegistrationDraftSubmitted>(draft.DomainEvents[^1]).RegistrationId);
        Assert.Equal(DomainErrorCodes.InvalidTransition,
            Assert.Throws<DomainException>(() => draft.SetEmail(EmailAddress.Create("other@example.com"), at.AddMinutes(1))).Code);
        Assert.Equal(DomainErrorCodes.InvalidTransition,
            Assert.Throws<DomainException>(() => draft.MarkSubmitted(id, at.AddMinutes(1))).Code);
        Assert.Equal(12L, draft.Version);
    }

    [Fact]
    public void Invalid_input_and_time_leave_draft_unchanged()
    {
        var draft = New();
        Assert.Equal(DomainErrorCodes.InvalidApplicantProfile,
            Assert.Throws<DomainException>(() => draft.SetFullName(null, T0.AddMinutes(1))).Code);
        Assert.Equal(DomainErrorCodes.InvalidTimestamp,
            Assert.Throws<DomainException>(() => draft.SetFullName(FullName.Create("Ana"), T0.AddMinutes(-1))).Code);
        Assert.Equal(DomainErrorCodes.InvalidTimestamp,
            Assert.Throws<DomainException>(() => draft.SetFullName(FullName.Create("Ana"),
                new DateTimeOffset(2026, 10, 12, 9, 1, 0, TimeSpan.FromHours(-3)))).Code);
        Assert.Null(draft.FullName);
        Assert.Equal(T0, draft.UpdatedAt);
        Assert.Equal(1L, draft.Version);
    }

    [Fact]
    public void Identity_is_by_draft_id_and_transient_events_can_be_cleared()
    {
        var id = RegistrationDraftId.From(Guid.NewGuid());
        var first = RegistrationDraft.Start(id, Owner, T0);
        var second = RegistrationDraft.Start(id, Owner, T0);
        second.SetFullName(FullName.Create("Ana"), T0.AddMinutes(1));
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        first.ClearDomainEvents();
        Assert.Empty(first.DomainEvents);
        Assert.Equal(1L, first.Version);
    }

    [Fact]
    public void Restore_preserves_partial_state_without_replaying_events()
    {
        var original = New();
        original.SetEmail(EmailAddress.Create("ana@example.com"), T0.AddMinutes(1));
        var state = new RegistrationDraftState(original.Id, original.OwnerId, original.Status,
            original.CreatedAt, original.UpdatedAt, original.SubmittedAt, original.RegistrationId,
            original.Version, original.FullName, original.Cpf, original.BirthDate, original.Email,
            original.Phone, original.MotherName, original.Nationality, original.Address,
            original.Profession, original.MonthlyIncome);

        var restored = RegistrationDraft.Restore(state);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Email, restored.Email);
        Assert.Equal(original.Version, restored.Version);
        Assert.Equal(RegistrationDraftStep.FullName, restored.CurrentStep);
        Assert.Empty(restored.DomainEvents);
        Assert.Equal(DomainErrorCodes.InvalidTransition,
            Assert.Throws<DomainException>(() => RegistrationDraft.Restore(state with
            {
                Status = RegistrationDraftStatus.Submitted
            })).Code);
    }

    private static RegistrationDraft New() => RegistrationDraft.Start(RegistrationDraftId.From(Guid.NewGuid()), Owner, T0);

    private static void Fill(RegistrationDraft draft, ApplicantProfile profile)
    {
        draft.SetFullName(profile.FullName, T0.AddMinutes(1));
        draft.SetCpf(profile.Cpf, T0.AddMinutes(2));
        draft.SetBirthDate(profile.BirthDate, T0.AddMinutes(3));
        draft.SetEmail(profile.Email, T0.AddMinutes(4));
        draft.SetPhone(profile.Phone, T0.AddMinutes(5));
        draft.SetMotherName(profile.MotherName, T0.AddMinutes(6));
        draft.SetNationality(profile.Nationality, T0.AddMinutes(7));
        draft.SetAddress(profile.Address, T0.AddMinutes(8));
        draft.SetProfession(profile.Profession, T0.AddMinutes(9));
        draft.SetMonthlyIncome(profile.MonthlyIncome, T0.AddMinutes(10));
    }
}
