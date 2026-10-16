using Registration.Application;
using Registration.Application.Registrations.SubmitDraft;
using Registration.Application.Tests.Support;
using Registration.Domain;
using Registration.Domain.Drafts;
using Registration.Domain.Drafts.Events;
using Registration.Domain.Registrations;
using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Applicants;

namespace Registration.Application.Tests.Registrations;

public sealed class SubmitRegistrationDraftTests
{
    [Fact]
    public async Task Complete_draft_creates_registration_then_links_and_persists_draft()
    {
        var draft = Sample.Draft();
        Sample.Complete(draft);
        var drafts = new FakeDraftRepository { Draft = draft };
        var registrations = new FakeRegistrationRepository
        {
            OnAdd = registration =>
            {
                Assert.Equal(RegistrationDraftStatus.InProgress, draft.Status);
                Assert.Equal(draft.Cpf, registration.Applicant.Cpf);
            }
        };
        var handler = new SubmitRegistrationDraftHandler(drafts, registrations, new FixedTimeProvider(Sample.T1));

        var result = await handler.HandleAsync(new(draft.Id, Sample.Owner));

        Assert.Equal(draft.Cpf, registrations.CheckedCpf);
        Assert.Equal(1, registrations.AddCalls);
        Assert.Equal(1, drafts.UpdateCalls);
        Assert.NotNull(registrations.Added);
        Assert.Equal(result.RegistrationId, registrations.Added.Id);
        Assert.Equal(RegistrationStatus.Received, registrations.Added.Status);
        Assert.Equal(RegistrationDraftStatus.Submitted, draft.Status);
        Assert.Equal(result.RegistrationId, draft.RegistrationId);
        Assert.Equal(Sample.T1, draft.SubmittedAt);
        Assert.IsType<RegistrationDraftSubmitted>(draft.DomainEvents[^1]);
    }

    [Fact]
    public async Task Incomplete_draft_does_not_query_cpf_or_persist()
    {
        var draft = Sample.Draft();
        var drafts = new FakeDraftRepository { Draft = draft };
        var registrations = new FakeRegistrationRepository();
        var handler = new SubmitRegistrationDraftHandler(drafts, registrations, new FixedTimeProvider(Sample.T1));

        var error = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new(draft.Id, Sample.Owner)));
        Assert.Equal(DomainErrorCodes.IncompleteDraft, error.Code);
        Assert.Null(registrations.CheckedCpf);
        Assert.Equal(0, registrations.AddCalls);
        Assert.Equal(0, drafts.UpdateCalls);
    }

    [Fact]
    public async Task Duplicate_cpf_does_not_create_registration_or_submit_draft()
    {
        var draft = Sample.Draft();
        Sample.Complete(draft);
        var drafts = new FakeDraftRepository { Draft = draft };
        var registrations = new FakeRegistrationRepository { ExistingCpf = true };
        var handler = new SubmitRegistrationDraftHandler(drafts, registrations, new FixedTimeProvider(Sample.T1));

        var error = await Assert.ThrowsAsync<RegistrationApplicationException>(() => handler.HandleAsync(new(draft.Id, Sample.Owner)));
        Assert.Equal(ApplicationErrorCodes.CpfAlreadyRegistered, error.Code);
        Assert.Equal(RegistrationDraftStatus.InProgress, draft.Status);
        Assert.Equal(0, registrations.AddCalls);
        Assert.Equal(0, drafts.UpdateCalls);
    }

    [Fact]
    public async Task Retry_after_oracle_write_links_existing_registration_without_duplicate()
    {
        var draft = Sample.Draft();
        Sample.Complete(draft);
        var existingId = RegistrationId.From(Guid.NewGuid());
        var drafts = new FakeDraftRepository { Draft = draft };
        var registrations = new FakeRegistrationRepository
        {
            ExistingDraftRegistration = new RegistrationSubmission(existingId, draft.ToApplicantProfile(DateOnly.FromDateTime(Sample.T1.UtcDateTime))),
            ExistingCpf = true
        };
        var handler = new SubmitRegistrationDraftHandler(drafts, registrations, new FixedTimeProvider(Sample.T1));

        var result = await handler.HandleAsync(new(draft.Id, Sample.Owner));

        Assert.Equal(existingId, result.RegistrationId);
        Assert.Equal(existingId, draft.RegistrationId);
        Assert.Equal(RegistrationDraftStatus.Submitted, draft.Status);
        Assert.Null(registrations.CheckedCpf);
        Assert.Equal(0, registrations.AddCalls);
        Assert.Equal(1, drafts.UpdateCalls);
    }

    [Fact]
    public async Task Retry_rejects_draft_changed_after_registration_was_written()
    {
        var draft = Sample.Draft();
        Sample.Complete(draft);
        var registeredProfile = draft.ToApplicantProfile(DateOnly.FromDateTime(Sample.T1.UtcDateTime));
        draft.SetEmail(EmailAddress.Create("outro@example.com"), Sample.T1);
        var drafts = new FakeDraftRepository { Draft = draft };
        var registrations = new FakeRegistrationRepository
        {
            ExistingDraftRegistration = new RegistrationSubmission(RegistrationId.From(Guid.NewGuid()), registeredProfile)
        };
        var handler = new SubmitRegistrationDraftHandler(drafts, registrations, new FixedTimeProvider(Sample.T1));

        var error = await Assert.ThrowsAsync<RegistrationApplicationException>(() =>
            handler.HandleAsync(new(draft.Id, Sample.Owner)));

        Assert.Equal(ApplicationErrorCodes.ConcurrencyConflict, error.Code);
        Assert.Equal(RegistrationDraftStatus.InProgress, draft.Status);
        Assert.Equal(0, drafts.UpdateCalls);
    }

    [Fact]
    public async Task Retry_after_draft_write_failure_reuses_the_oracle_registration()
    {
        var draft = Sample.Draft();
        Sample.Complete(draft);
        var persistedDraft = RegistrationDraft.Restore(new RegistrationDraftState(
            draft.Id, draft.OwnerId, draft.Status, draft.CreatedAt, draft.UpdatedAt,
            draft.SubmittedAt, draft.RegistrationId, draft.Version, draft.FullName, draft.Cpf,
            draft.BirthDate, draft.Email, draft.Phone, draft.MotherName, draft.Nationality,
            draft.Address, draft.Profession, draft.MonthlyIncome));
        var drafts = new FakeDraftRepository { Draft = draft, FailNextUpdate = true };
        var registrations = new FakeRegistrationRepository();
        var handler = new SubmitRegistrationDraftHandler(drafts, registrations, new FixedTimeProvider(Sample.T1));

        await Assert.ThrowsAsync<IOException>(() => handler.HandleAsync(new(draft.Id, Sample.Owner)));
        drafts.Draft = persistedDraft;
        var retry = await handler.HandleAsync(new(draft.Id, Sample.Owner));

        Assert.Equal(1, registrations.AddCalls);
        Assert.Equal(registrations.Added?.Id, retry.RegistrationId);
        Assert.Equal(retry.RegistrationId, persistedDraft.RegistrationId);
        Assert.Equal(RegistrationDraftStatus.Submitted, persistedDraft.Status);
        Assert.Equal(1, drafts.UpdateCalls);
    }

    [Fact]
    public async Task Stale_submission_time_is_rejected_before_any_registration_write()
    {
        var draft = Sample.Draft();
        Sample.Complete(draft);
        var drafts = new FakeDraftRepository { Draft = draft };
        var registrations = new FakeRegistrationRepository();
        var handler = new SubmitRegistrationDraftHandler(drafts, registrations, new FixedTimeProvider(Sample.T0));

        var error = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new(draft.Id, Sample.Owner)));
        Assert.Equal(DomainErrorCodes.InvalidTimestamp, error.Code);
        Assert.Null(registrations.CheckedCpf);
        Assert.Equal(0, registrations.AddCalls);
        Assert.Equal(0, drafts.UpdateCalls);
    }

    [Fact]
    public async Task Missing_or_unowned_draft_cannot_be_submitted()
    {
        var draft = Sample.Draft();
        Sample.Complete(draft);
        var drafts = new FakeDraftRepository { Draft = draft };
        var registrations = new FakeRegistrationRepository();
        var handler = new SubmitRegistrationDraftHandler(drafts, registrations, new FixedTimeProvider(Sample.T1));

        var denied = await Assert.ThrowsAsync<RegistrationApplicationException>(() =>
            handler.HandleAsync(new(draft.Id, Sample.OtherOwner)));
        Assert.Equal(ApplicationErrorCodes.DraftAccessDenied, denied.Code);
        var missing = await Assert.ThrowsAsync<RegistrationApplicationException>(() =>
            handler.HandleAsync(new(RegistrationDraftId.From(Guid.NewGuid()), Sample.Owner)));
        Assert.Equal(ApplicationErrorCodes.DraftNotFound, missing.Code);
        Assert.Null(registrations.CheckedCpf);
    }

    [Fact]
    public async Task Submitted_draft_cannot_create_second_registration()
    {
        var draft = Sample.Draft();
        Sample.Complete(draft);
        draft.MarkSubmitted(RegistrationId.From(Guid.NewGuid()), Sample.T1);
        var drafts = new FakeDraftRepository { Draft = draft };
        var registrations = new FakeRegistrationRepository();
        var handler = new SubmitRegistrationDraftHandler(drafts, registrations, new FixedTimeProvider(Sample.T1));

        var error = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new(draft.Id, Sample.Owner)));
        Assert.Equal(DomainErrorCodes.InvalidTransition, error.Code);
        Assert.Equal(0, registrations.AddCalls);
    }
}
