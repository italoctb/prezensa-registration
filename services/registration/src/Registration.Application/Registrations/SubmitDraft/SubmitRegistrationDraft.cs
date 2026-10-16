using Registration.Application.Abstractions.Persistence;
using Registration.Application.Drafts;
using Registration.Domain.Drafts;
using Registration.Domain.Registrations;
using RegistrationAggregate = Registration.Domain.Registrations.Registration;

namespace Registration.Application.Registrations.SubmitDraft;

public sealed record SubmitRegistrationDraftCommand(RegistrationDraftId DraftId, OwnerId OwnerId);
public sealed record SubmitRegistrationDraftResult(RegistrationId RegistrationId);

public sealed class SubmitRegistrationDraftHandler(
    IRegistrationDraftRepository drafts, IRegistrationRepository registrations, TimeProvider timeProvider)
{
    public async Task<SubmitRegistrationDraftResult> HandleAsync(
        SubmitRegistrationDraftCommand command, CancellationToken cancellationToken = default)
    {
        var draft = await DraftAccess.LoadOwnedAsync(drafts, command.DraftId, command.OwnerId, cancellationToken);
        var expectedVersion = draft.Version;
        var now = timeProvider.GetUtcNow();
        draft.EnsureCanSubmitAt(now);
        var profile = draft.ToApplicantProfile(DateOnly.FromDateTime(now.UtcDateTime));
        var existing = await registrations.FindByDraftIdAsync(draft.Id, cancellationToken);
        if (existing is not null)
        {
            if (existing.Applicant != profile)
                throw new RegistrationApplicationException(ApplicationErrorCodes.ConcurrencyConflict,
                    "Draft differs from the registration already created for it.");
            draft.MarkSubmitted(existing.RegistrationId, now);
            await drafts.UpdateAsync(draft, expectedVersion, cancellationToken);
            return new SubmitRegistrationDraftResult(existing.RegistrationId);
        }
        if (await registrations.ExistsByCpfAsync(profile.Cpf, cancellationToken))
            throw new RegistrationApplicationException(ApplicationErrorCodes.CpfAlreadyRegistered, "CPF is already registered.");

        var registrationId = RegistrationId.From(Guid.NewGuid());
        var registration = RegistrationAggregate.Create(registrationId, profile, now);
        var persistedId = await registrations.AddForDraftAsync(registration, draft.Id, cancellationToken);
        draft.MarkSubmitted(persistedId, now);
        await drafts.UpdateAsync(draft, expectedVersion, cancellationToken);
        return new SubmitRegistrationDraftResult(persistedId);
    }
}
