using Registration.Domain.Registrations;

namespace Registration.Domain.Drafts.Events;

public abstract record RegistrationDraftDomainEvent(RegistrationDraftId DraftId, DateTimeOffset OccurredAt);

public sealed record RegistrationDraftStarted(RegistrationDraftId DraftId, OwnerId OwnerId, DateTimeOffset OccurredAt)
    : RegistrationDraftDomainEvent(DraftId, OccurredAt);

public sealed record RegistrationDraftSubmitted(RegistrationDraftId DraftId, RegistrationId RegistrationId, DateTimeOffset OccurredAt)
    : RegistrationDraftDomainEvent(DraftId, OccurredAt);
