namespace Registration.Domain.Registrations.Events;

public abstract record RegistrationDomainEvent(RegistrationId RegistrationId, DateTimeOffset OccurredAt);

public sealed record RegistrationReceived(RegistrationId RegistrationId, DateTimeOffset OccurredAt)
    : RegistrationDomainEvent(RegistrationId, OccurredAt);

public sealed record RegistrationProcessingStarted(RegistrationId RegistrationId, DateTimeOffset OccurredAt)
    : RegistrationDomainEvent(RegistrationId, OccurredAt);

public sealed record FraudCheckRequested(RegistrationId RegistrationId, DateTimeOffset OccurredAt)
    : RegistrationDomainEvent(RegistrationId, OccurredAt);

public sealed record FraudCheckApproved(RegistrationId RegistrationId, DateTimeOffset OccurredAt)
    : RegistrationDomainEvent(RegistrationId, OccurredAt);

public sealed record FraudCheckRejected(RegistrationId RegistrationId, DateTimeOffset OccurredAt)
    : RegistrationDomainEvent(RegistrationId, OccurredAt);

public sealed record RegistrationCompleted(RegistrationId RegistrationId, ApprovalActor ApprovedBy, DateTimeOffset OccurredAt)
    : RegistrationDomainEvent(RegistrationId, OccurredAt);

public sealed record RegistrationFailed(RegistrationId RegistrationId, FailureReason Reason, DateTimeOffset OccurredAt)
    : RegistrationDomainEvent(RegistrationId, OccurredAt);

public sealed record RegistrationCancelled(RegistrationId RegistrationId, DateTimeOffset OccurredAt)
    : RegistrationDomainEvent(RegistrationId, OccurredAt);
