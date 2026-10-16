using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;
using Registration.Domain.Drafts.Events;
using Registration.Domain.Registrations;

namespace Registration.Domain.Drafts;

public sealed class RegistrationDraft : IEquatable<RegistrationDraft>
{
    private readonly List<RegistrationDraftDomainEvent> _domainEvents = [];

    private RegistrationDraft(RegistrationDraftId id, OwnerId ownerId, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerId = ownerId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Version = 1;
        Status = RegistrationDraftStatus.InProgress;
        _domainEvents.Add(new RegistrationDraftStarted(id, ownerId, createdAt));
    }

    public RegistrationDraftId Id { get; }
    public OwnerId OwnerId { get; }
    public RegistrationDraftStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public RegistrationId? RegistrationId { get; private set; }
    public long Version { get; private set; }
    public FullName? FullName { get; private set; }
    public Cpf? Cpf { get; private set; }
    public BirthDate? BirthDate { get; private set; }
    public EmailAddress? Email { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public MotherName? MotherName { get; private set; }
    public Nationality? Nationality { get; private set; }
    public Address? Address { get; private set; }
    public Profession? Profession { get; private set; }
    public MonthlyIncome? MonthlyIncome { get; private set; }
    public IReadOnlyList<RegistrationDraftDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public RegistrationDraftStep CurrentStep =>
        FullName is null ? RegistrationDraftStep.FullName :
        Cpf is null ? RegistrationDraftStep.Document :
        BirthDate is null ? RegistrationDraftStep.BirthDate :
        Email is null ? RegistrationDraftStep.Email :
        Phone is null ? RegistrationDraftStep.Phone :
        MotherName is null ? RegistrationDraftStep.MotherName :
        Nationality is null ? RegistrationDraftStep.Nationality :
        Address is null ? RegistrationDraftStep.Address :
        Profession is null ? RegistrationDraftStep.Profession :
        MonthlyIncome is null ? RegistrationDraftStep.MonthlyIncome : RegistrationDraftStep.Review;

    public static RegistrationDraft Start(RegistrationDraftId id, OwnerId ownerId, DateTimeOffset occurredAt)
    {
        if (id.Value == Guid.Empty || ownerId.Value == Guid.Empty)
            throw new DomainException(DomainErrorCodes.InvalidId, "Draft and owner IDs cannot be empty.");
        RequireUtc(occurredAt);
        return new RegistrationDraft(id, ownerId, occurredAt);
    }

    public static RegistrationDraft Restore(RegistrationDraftState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var draft = Start(state.Id, state.OwnerId, state.CreatedAt);
        RequireUtc(state.UpdatedAt);
        if (state.UpdatedAt < state.CreatedAt || state.Version < 1 ||
            (state.Version == 1 && state.UpdatedAt != state.CreatedAt))
            throw new DomainException(DomainErrorCodes.InvalidTimestamp, "Draft state has inconsistent time or version.");

        draft.FullName = state.FullName;
        draft.Cpf = state.Cpf;
        draft.BirthDate = state.BirthDate;
        draft.Email = state.Email;
        draft.Phone = state.Phone;
        draft.MotherName = state.MotherName;
        draft.Nationality = state.Nationality;
        draft.Address = state.Address;
        draft.Profession = state.Profession;
        draft.MonthlyIncome = state.MonthlyIncome;
        draft.UpdatedAt = state.UpdatedAt;
        draft.Version = state.Version;

        if (state.Status == RegistrationDraftStatus.InProgress &&
            (state.SubmittedAt is not null || state.RegistrationId is not null))
            throw new DomainException(DomainErrorCodes.InvalidTransition, "Active draft cannot have submission data.");
        if (state.Status == RegistrationDraftStatus.Submitted)
        {
            if (state.SubmittedAt is null || state.SubmittedAt != state.UpdatedAt ||
                state.RegistrationId is null || state.RegistrationId.Value.Value == Guid.Empty ||
                state.Version < 2)
                throw new DomainException(DomainErrorCodes.InvalidTransition, "Submitted draft requires registration, time and version.");
            RequireUtc(state.SubmittedAt.Value);
            draft.EnsureCanSubmit(DateOnly.FromDateTime(state.SubmittedAt.Value.UtcDateTime));
        }
        if (!Enum.IsDefined(state.Status))
            throw new DomainException(DomainErrorCodes.InvalidTransition, "Unknown draft state.");

        draft.Status = state.Status;
        draft.SubmittedAt = state.SubmittedAt;
        draft.RegistrationId = state.RegistrationId;
        draft.ClearDomainEvents();
        return draft;
    }

    public void SetFullName(FullName? value, DateTimeOffset at) => Change(value, at, v => FullName = v);
    public void SetCpf(Cpf? value, DateTimeOffset at) => Change(value, at, v => Cpf = v);
    public void SetBirthDate(BirthDate? value, DateTimeOffset at) => Change(value, at, v => BirthDate = v);
    public void SetEmail(EmailAddress? value, DateTimeOffset at) => Change(value, at, v => Email = v);
    public void SetPhone(PhoneNumber? value, DateTimeOffset at) => Change(value, at, v => Phone = v);
    public void SetMotherName(MotherName? value, DateTimeOffset at) => Change(value, at, v => MotherName = v);
    public void SetNationality(Nationality? value, DateTimeOffset at) => Change(value, at, v => Nationality = v);
    public void SetAddress(Address? value, DateTimeOffset at) => Change(value, at, v => Address = v);
    public void SetProfession(Profession? value, DateTimeOffset at) => Change(value, at, v => Profession = v);
    public void SetMonthlyIncome(MonthlyIncome? value, DateTimeOffset at) => Change(value, at, v => MonthlyIncome = v);

    public bool CanSubmit(DateOnly referenceDate) =>
        Status == RegistrationDraftStatus.InProgress &&
        CurrentStep == RegistrationDraftStep.Review && BirthDate is not null &&
        BirthDate.Value <= referenceDate && BirthDate.IsAtLeast18On(referenceDate);

    public ApplicantProfile ToApplicantProfile(DateOnly referenceDate)
    {
        EnsureCanSubmit(referenceDate);
        return ApplicantProfile.Create(FullName, Cpf, BirthDate, Email, Phone, MotherName,
            Nationality, Address, Profession, MonthlyIncome);
    }

    public void EnsureCanSubmit(DateOnly referenceDate)
    {
        RequireInProgress();
        if (CurrentStep != RegistrationDraftStep.Review)
            throw new DomainException(DomainErrorCodes.IncompleteDraft, "Draft is missing required applicant data.");
        if (BirthDate!.Value > referenceDate)
            throw new DomainException(DomainErrorCodes.InvalidBirthDate, "Birth date cannot be in the future.");
        if (!BirthDate.IsAtLeast18On(referenceDate))
            throw new DomainException(DomainErrorCodes.UnderageApplicant, "Applicant must be at least 18 years old.");
    }

    public void EnsureCanSubmitAt(DateTimeOffset occurredAt)
    {
        RequireUtc(occurredAt);
        if (occurredAt < UpdatedAt)
            throw new DomainException(DomainErrorCodes.InvalidTimestamp, "Submission time cannot precede the last change.");
        EnsureCanSubmit(DateOnly.FromDateTime(occurredAt.UtcDateTime));
    }

    public void MarkSubmitted(RegistrationId registrationId, DateTimeOffset occurredAt)
    {
        RequireInProgress();
        if (registrationId.Value == Guid.Empty)
            throw new DomainException(DomainErrorCodes.InvalidId, "Registration ID cannot be empty.");
        EnsureCanSubmitAt(occurredAt);
        Advance(occurredAt);
        Status = RegistrationDraftStatus.Submitted;
        RegistrationId = registrationId;
        SubmittedAt = occurredAt;
        _domainEvents.Add(new RegistrationDraftSubmitted(Id, registrationId, occurredAt));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
    public bool Equals(RegistrationDraft? other) => other is not null && Id == other.Id;
    public override bool Equals(object? obj) => obj is RegistrationDraft other && Equals(other);
    public override int GetHashCode() => Id.GetHashCode();

    private void Change<T>(T? value, DateTimeOffset at, Action<T> assign) where T : class
    {
        RequireInProgress();
        if (value is null)
            throw new DomainException(DomainErrorCodes.InvalidApplicantProfile, "Draft field cannot be cleared with a null value.");
        Advance(at);
        assign(value);
    }

    private void RequireInProgress()
    {
        if (Status != RegistrationDraftStatus.InProgress)
            throw new DomainException(DomainErrorCodes.InvalidTransition, "Submitted draft cannot be changed or submitted again.");
    }

    private void Advance(DateTimeOffset at)
    {
        RequireUtc(at);
        if (at < UpdatedAt)
            throw new DomainException(DomainErrorCodes.InvalidTimestamp, "Event time cannot precede the last change.");
        var nextVersion = checked(Version + 1);
        UpdatedAt = at;
        Version = nextVersion;
    }

    private static void RequireUtc(DateTimeOffset at)
    {
        if (at.Offset != TimeSpan.Zero)
            throw new DomainException(DomainErrorCodes.InvalidTimestamp, "Event time must use UTC offset zero.");
    }
}
