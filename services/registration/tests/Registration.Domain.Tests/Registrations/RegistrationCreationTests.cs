using Registration.Domain.Registrations;
using Registration.Domain.Applicants;
using Registration.Domain.Registrations.Events;
using RegistrationAggregate = global::Registration.Domain.Registrations.Registration;

namespace Registration.Domain.Tests.Registrations;

public sealed class RegistrationCreationTests
{
    [Fact]
    public void Creation_captures_complete_applicant_and_initial_state()
    {
        var applicant = RegistrationSample.Applicant();
        var registration = RegistrationSample.New(applicant: applicant);

        Assert.Equal(applicant, registration.Applicant);
        Assert.Equal(RegistrationStatus.Received, registration.Status);
        Assert.Equal(FraudCheckStatus.NotRequested, registration.FraudStatus);
        Assert.Equal(RegistrationSample.T0, registration.CreatedAt);
        Assert.Equal(RegistrationSample.T0, registration.UpdatedAt);
        Assert.Equal(1L, registration.Version);
        Assert.Null(registration.ApprovedAt);
        Assert.Null(registration.ApprovedBy);
        Assert.Null(registration.FailureReason);
        Assert.Equal(registration.Id,
            Assert.IsType<RegistrationReceived>(Assert.Single(registration.DomainEvents)).RegistrationId);
    }

    [Fact]
    public void Creation_rejects_default_id_and_missing_applicant()
    {
        Assert.Equal(DomainErrorCodes.InvalidId,
            Assert.Throws<DomainException>(() => RegistrationAggregate.Create(default, RegistrationSample.Applicant(), RegistrationSample.T0)).Code);
        Assert.Equal(DomainErrorCodes.InvalidApplicantProfile,
            Assert.Throws<DomainException>(() => RegistrationAggregate.Create(RegistrationId.From(Guid.NewGuid()), null, RegistrationSample.T0)).Code);
    }

    [Fact]
    public void Exactly_eighteen_years_old_is_eligible()
    {
        var applicant = RegistrationSample.Applicant(new DateOnly(2008, 10, 12));
        Assert.Equal(RegistrationStatus.Received, RegistrationSample.New(applicant: applicant).Status);
    }

    [Fact]
    public void Underage_applicant_is_rejected()
    {
        var applicant = RegistrationSample.Applicant(new DateOnly(2008, 10, 13));
        Assert.Equal(DomainErrorCodes.UnderageApplicant,
            Assert.Throws<DomainException>(() => RegistrationSample.New(applicant: applicant)).Code);
    }

    [Fact]
    public void Future_birth_date_is_rejected_separately()
    {
        var applicant = RegistrationSample.Applicant(new DateOnly(2026, 10, 13));
        Assert.Equal(DomainErrorCodes.InvalidBirthDate,
            Assert.Throws<DomainException>(() => RegistrationSample.New(applicant: applicant)).Code);
    }

    [Fact]
    public void Creation_requires_utc()
    {
        var localTime = new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.FromHours(-3));
        Assert.Equal(DomainErrorCodes.InvalidTimestamp,
            Assert.Throws<DomainException>(() => RegistrationAggregate.Create(
                RegistrationId.From(Guid.NewGuid()), RegistrationSample.Applicant(), localTime)).Code);
    }

    [Fact]
    public void Registrations_compare_by_identity_not_snapshot()
    {
        var id = RegistrationId.From(Guid.NewGuid());
        var first = RegistrationSample.New(id);
        var second = RegistrationSample.New(id);
        second.StartProcessing(RegistrationSample.T1);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, RegistrationSample.New());
    }

    [Fact]
    public void Restore_preserves_processing_state_without_replaying_events()
    {
        var original = RegistrationSample.PendingFraud();
        var state = new RegistrationState(original.Id, original.Applicant, original.Status,
            original.FraudStatus, original.FailureReason, original.CreatedAt, original.UpdatedAt,
            original.ApprovedAt, original.ApprovedBy, original.Version);
        var restored = RegistrationAggregate.Restore(state);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.FraudStatus, restored.FraudStatus);
        Assert.Equal(original.Version, restored.Version);
        Assert.Empty(restored.DomainEvents);
        Assert.Equal(DomainErrorCodes.InvalidTransition,
            Assert.Throws<DomainException>(() => RegistrationAggregate.Restore(state with
            {
                Status = RegistrationStatus.Completed
            })).Code);
    }
}
