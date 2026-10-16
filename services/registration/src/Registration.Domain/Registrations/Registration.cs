using Registration.Domain.Applicants;
using Registration.Domain.Registrations.Events;

namespace Registration.Domain.Registrations;

public sealed class Registration : IEquatable<Registration>
{
    private readonly List<RegistrationDomainEvent> _domainEvents = [];

    private Registration(RegistrationId id, ApplicantProfile applicant, DateTimeOffset createdAt)
    {
        Id = id;
        Applicant = applicant;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Version = 1;
        Status = RegistrationStatus.Received;
        FraudStatus = FraudCheckStatus.NotRequested;
        _domainEvents.Add(new RegistrationReceived(id, createdAt));
    }

    public RegistrationId Id { get; }
    public ApplicantProfile Applicant { get; }
    public RegistrationStatus Status { get; private set; }
    public FraudCheckStatus FraudStatus { get; private set; }
    public FailureReason? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public ApprovalActor? ApprovedBy { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyList<RegistrationDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Registration Create(RegistrationId id, ApplicantProfile? applicant, DateTimeOffset occurredAt)
    {
        if (id.Value == Guid.Empty)
            throw new DomainException(DomainErrorCodes.InvalidId, "Registration ID cannot be empty.");
        if (applicant is null)
            throw new DomainException(DomainErrorCodes.InvalidApplicantProfile, "Applicant profile is required.");

        RequireUtc(occurredAt);
        var today = DateOnly.FromDateTime(occurredAt.UtcDateTime);
        if (applicant.BirthDate.Value > today)
            throw new DomainException(DomainErrorCodes.InvalidBirthDate, "Birth date cannot be in the future.");
        if (!applicant.BirthDate.IsAtLeast18On(today))
            throw new DomainException(DomainErrorCodes.UnderageApplicant, "Applicant must be at least 18 years old.");

        return new Registration(id, applicant, occurredAt);
    }

    public static Registration Restore(RegistrationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var registration = Create(state.Id, state.Applicant, state.CreatedAt);
        RequireUtc(state.UpdatedAt);
        if (state.UpdatedAt < state.CreatedAt || state.Version < 1 ||
            (state.Version == 1 && (state.Status != RegistrationStatus.Received || state.UpdatedAt != state.CreatedAt)) ||
            !Enum.IsDefined(state.Status) || !Enum.IsDefined(state.FraudStatus))
            throw new DomainException(DomainErrorCodes.InvalidTransition, "Registration state has inconsistent time, version or status.");
        if ((state.Status == RegistrationStatus.Received && state.FraudStatus != FraudCheckStatus.NotRequested) ||
            (state.Status == RegistrationStatus.Rejected) != (state.FraudStatus == FraudCheckStatus.Rejected) ||
            (state.Status == RegistrationStatus.Failed) != (state.FailureReason is not null) ||
            (state.Status == RegistrationStatus.Completed) != (state.ApprovedAt is not null && state.ApprovedBy is not null))
            throw new DomainException(DomainErrorCodes.InvalidTransition, "Registration state has inconsistent business fields.");
        if (state.Status == RegistrationStatus.Completed &&
            (state.FraudStatus != FraudCheckStatus.Approved || state.ApprovedAt != state.UpdatedAt))
            throw new DomainException(DomainErrorCodes.InvalidTransition, "Completed registration requires fraud approval and matching approval time.");

        registration.Status = state.Status;
        registration.FraudStatus = state.FraudStatus;
        registration.FailureReason = state.FailureReason;
        registration.UpdatedAt = state.UpdatedAt;
        registration.ApprovedAt = state.ApprovedAt;
        registration.ApprovedBy = state.ApprovedBy;
        registration.Version = state.Version;
        registration.ClearDomainEvents();
        return registration;
    }

    public void StartProcessing(DateTimeOffset occurredAt)
    {
        RequireState(Status == RegistrationStatus.Received, "start processing");
        Advance(occurredAt);
        Status = RegistrationStatus.Processing;
        _domainEvents.Add(new RegistrationProcessingStarted(Id, occurredAt));
    }

    public void RequestFraudCheck(DateTimeOffset occurredAt)
    {
        RequireState(Status == RegistrationStatus.Processing && FraudStatus == FraudCheckStatus.NotRequested, "request a fraud check");
        Advance(occurredAt);
        FraudStatus = FraudCheckStatus.Pending;
        _domainEvents.Add(new FraudCheckRequested(Id, occurredAt));
    }

    public void ApproveFraud(DateTimeOffset occurredAt)
    {
        RequireState(Status == RegistrationStatus.Processing && FraudStatus == FraudCheckStatus.Pending, "approve fraud check");
        Advance(occurredAt);
        FraudStatus = FraudCheckStatus.Approved;
        _domainEvents.Add(new FraudCheckApproved(Id, occurredAt));
    }

    public void RejectFraud(DateTimeOffset occurredAt)
    {
        RequireState(Status == RegistrationStatus.Processing && FraudStatus == FraudCheckStatus.Pending, "reject fraud check");
        Advance(occurredAt);
        FraudStatus = FraudCheckStatus.Rejected;
        Status = RegistrationStatus.Rejected;
        _domainEvents.Add(new FraudCheckRejected(Id, occurredAt));
    }

    public void Complete(ApprovalActor? approvedBy, DateTimeOffset occurredAt)
    {
        RequireState(Status == RegistrationStatus.Processing && FraudStatus == FraudCheckStatus.Approved, "complete registration");
        if (approvedBy is null)
            throw new DomainException(DomainErrorCodes.InvalidApprovalActor, "Approval actor is required.");

        Advance(occurredAt);
        Status = RegistrationStatus.Completed;
        ApprovedAt = occurredAt;
        ApprovedBy = approvedBy;
        _domainEvents.Add(new RegistrationCompleted(Id, approvedBy, occurredAt));
    }

    public void Fail(FailureReason? reason, DateTimeOffset occurredAt)
    {
        RequireState(Status is RegistrationStatus.Received or RegistrationStatus.Processing, "fail registration");
        if (reason is null)
            throw new DomainException(DomainErrorCodes.InvalidFailureReason, "Failure reason is required.");

        Advance(occurredAt);
        FailureReason = reason;
        Status = RegistrationStatus.Failed;
        _domainEvents.Add(new RegistrationFailed(Id, reason, occurredAt));
    }

    public void Cancel(DateTimeOffset occurredAt)
    {
        RequireState(Status is RegistrationStatus.Received or RegistrationStatus.Processing, "cancel registration");
        Advance(occurredAt);
        Status = RegistrationStatus.Cancelled;
        _domainEvents.Add(new RegistrationCancelled(Id, occurredAt));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool Equals(Registration? other) => other is not null && Id == other.Id;

    public override bool Equals(object? obj) => obj is Registration other && Equals(other);

    public override int GetHashCode() => Id.GetHashCode();

    private void Advance(DateTimeOffset occurredAt)
    {
        RequireUtc(occurredAt);
        if (occurredAt < UpdatedAt)
            throw new DomainException(DomainErrorCodes.InvalidTimestamp, "Event time cannot precede the last change.");

        var nextVersion = checked(Version + 1);
        UpdatedAt = occurredAt;
        Version = nextVersion;
    }

    private static void RequireUtc(DateTimeOffset occurredAt)
    {
        if (occurredAt.Offset != TimeSpan.Zero)
            throw new DomainException(DomainErrorCodes.InvalidTimestamp, "Event time must use UTC offset zero.");
    }

    private static void RequireState(bool valid, string operation)
    {
        if (!valid)
            throw new DomainException(DomainErrorCodes.InvalidTransition, $"Cannot {operation} from the current registration state.");
    }
}
