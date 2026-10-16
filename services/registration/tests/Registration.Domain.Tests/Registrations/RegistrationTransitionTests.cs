using Registration.Domain.Registrations;
using Registration.Domain.Registrations.Events;

namespace Registration.Domain.Tests.Registrations;

public sealed class RegistrationTransitionTests
{
    [Fact]
    public void Approval_requires_fraud_clearance_and_records_actor_time_version_and_events()
    {
        var registration = RegistrationSample.New();
        var approver = RegistrationSample.Approver();
        registration.StartProcessing(RegistrationSample.T1);
        registration.RequestFraudCheck(RegistrationSample.T2);
        RegistrationSample.AssertInvalidTransition(() => registration.Complete(approver, RegistrationSample.T3));
        registration.ApproveFraud(RegistrationSample.T3);
        registration.Complete(approver, RegistrationSample.T4);

        Assert.Equal(RegistrationStatus.Completed, registration.Status);
        Assert.Equal(FraudCheckStatus.Approved, registration.FraudStatus);
        Assert.Equal(RegistrationSample.T4, registration.ApprovedAt);
        Assert.Equal(approver, registration.ApprovedBy);
        Assert.Equal(RegistrationSample.T4, registration.UpdatedAt);
        Assert.Equal(5L, registration.Version);
        Assert.Equal(approver, Assert.IsType<RegistrationCompleted>(registration.DomainEvents[^1]).ApprovedBy);
        Assert.Collection(registration.DomainEvents,
            e => Assert.IsType<RegistrationReceived>(e),
            e => Assert.IsType<RegistrationProcessingStarted>(e),
            e => Assert.IsType<FraudCheckRequested>(e),
            e => Assert.IsType<FraudCheckApproved>(e),
            e => Assert.IsType<RegistrationCompleted>(e));
    }

    [Fact]
    public void Completion_requires_an_approval_actor_without_mutating_state()
    {
        var registration = RegistrationSample.PendingFraud();
        registration.ApproveFraud(RegistrationSample.T3);

        Assert.Equal(DomainErrorCodes.InvalidApprovalActor,
            Assert.Throws<DomainException>(() => registration.Complete(null, RegistrationSample.T4)).Code);
        Assert.Equal(RegistrationStatus.Processing, registration.Status);
        Assert.Null(registration.ApprovedAt);
        Assert.Null(registration.ApprovedBy);
        Assert.Equal(4L, registration.Version);
    }

    [Fact]
    public void Fraud_rejection_is_terminal_and_not_operational_failure()
    {
        var registration = RegistrationSample.PendingFraud();
        registration.RejectFraud(RegistrationSample.T3);

        Assert.Equal(RegistrationStatus.Rejected, registration.Status);
        Assert.Equal(FraudCheckStatus.Rejected, registration.FraudStatus);
        Assert.Null(registration.FailureReason);
        Assert.Null(registration.ApprovedAt);
        Assert.Null(registration.ApprovedBy);
        Assert.Equal(4L, registration.Version);
        Assert.IsType<FraudCheckRejected>(registration.DomainEvents[^1]);
        RegistrationSample.AssertInvalidTransition(() => registration.Complete(RegistrationSample.Approver(), RegistrationSample.T4));
        RegistrationSample.AssertInvalidTransition(() => registration.Cancel(RegistrationSample.T4));
        RegistrationSample.AssertInvalidTransition(() => registration.Fail(FailureReason.Create("x"), RegistrationSample.T4));
    }

    [Fact]
    public void Failure_records_reason_but_not_approval()
    {
        var registration = RegistrationSample.PendingFraud();
        var reason = FailureReason.Create("Provider unavailable");
        registration.Fail(reason, RegistrationSample.T3);

        Assert.Equal(RegistrationStatus.Failed, registration.Status);
        Assert.Equal(FraudCheckStatus.Pending, registration.FraudStatus);
        Assert.Equal(reason, registration.FailureReason);
        Assert.Null(registration.ApprovedAt);
        Assert.Null(registration.ApprovedBy);
        Assert.Equal(4L, registration.Version);
        Assert.Equal(reason, Assert.IsType<RegistrationFailed>(registration.DomainEvents[^1]).Reason);
        RegistrationSample.AssertInvalidTransition(() => registration.Complete(RegistrationSample.Approver(), RegistrationSample.T4));
        RegistrationSample.AssertInvalidTransition(() => registration.Cancel(RegistrationSample.T4));
    }

    [Fact]
    public void Failure_can_end_a_received_registration()
    {
        var registration = RegistrationSample.New();
        registration.Fail(FailureReason.Create("Unable to proceed"), RegistrationSample.T1);

        Assert.Equal(RegistrationStatus.Failed, registration.Status);
        Assert.Equal(2L, registration.Version);
        Assert.IsType<RegistrationFailed>(registration.DomainEvents[^1]);
    }

    [Fact]
    public void Failure_requires_reason_without_mutating_state()
    {
        var registration = RegistrationSample.New();
        Assert.Equal(DomainErrorCodes.InvalidFailureReason,
            Assert.Throws<DomainException>(() => registration.Fail(null, RegistrationSample.T1)).Code);
        Assert.Equal(RegistrationStatus.Received, registration.Status);
        Assert.Equal(1L, registration.Version);
        Assert.Single(registration.DomainEvents);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancellation_is_valid_before_and_during_processing(bool startProcessing)
    {
        var registration = RegistrationSample.New();
        if (startProcessing)
            registration.StartProcessing(RegistrationSample.T1);
        registration.Cancel(RegistrationSample.T2);

        Assert.Equal(RegistrationStatus.Cancelled, registration.Status);
        Assert.Null(registration.ApprovedAt);
        Assert.Null(registration.ApprovedBy);
        Assert.Equal(startProcessing ? 3L : 2L, registration.Version);
        Assert.IsType<RegistrationCancelled>(registration.DomainEvents[^1]);
        RegistrationSample.AssertInvalidTransition(() => registration.ApproveFraud(RegistrationSample.T3));
        RegistrationSample.AssertInvalidTransition(() => registration.Complete(RegistrationSample.Approver(), RegistrationSample.T3));
    }

    [Fact]
    public void Repeated_and_out_of_order_operations_are_rejected()
    {
        var registration = RegistrationSample.New();
        RegistrationSample.AssertInvalidTransition(() => registration.RequestFraudCheck(RegistrationSample.T1));
        RegistrationSample.AssertInvalidTransition(() => registration.ApproveFraud(RegistrationSample.T1));
        RegistrationSample.AssertInvalidTransition(() => registration.Complete(RegistrationSample.Approver(), RegistrationSample.T1));

        registration.StartProcessing(RegistrationSample.T1);
        RegistrationSample.AssertInvalidTransition(() => registration.StartProcessing(RegistrationSample.T2));
        registration.RequestFraudCheck(RegistrationSample.T2);
        RegistrationSample.AssertInvalidTransition(() => registration.RequestFraudCheck(RegistrationSample.T3));
        registration.ApproveFraud(RegistrationSample.T3);
        RegistrationSample.AssertInvalidTransition(() => registration.ApproveFraud(RegistrationSample.T4));
        RegistrationSample.AssertInvalidTransition(() => registration.RejectFraud(RegistrationSample.T4));
        Assert.Equal(4L, registration.Version);
    }

    [Fact]
    public void Completed_registration_rejects_every_transition()
    {
        var registration = RegistrationSample.PendingFraud();
        registration.ApproveFraud(RegistrationSample.T3);
        registration.Complete(RegistrationSample.Approver(), RegistrationSample.T4);
        var later = RegistrationSample.T4.AddMinutes(1);

        RegistrationSample.AssertInvalidTransition(() => registration.StartProcessing(later));
        RegistrationSample.AssertInvalidTransition(() => registration.RequestFraudCheck(later));
        RegistrationSample.AssertInvalidTransition(() => registration.ApproveFraud(later));
        RegistrationSample.AssertInvalidTransition(() => registration.RejectFraud(later));
        RegistrationSample.AssertInvalidTransition(() => registration.Complete(RegistrationSample.Approver(), later));
        RegistrationSample.AssertInvalidTransition(() => registration.Fail(FailureReason.Create("x"), later));
        RegistrationSample.AssertInvalidTransition(() => registration.Cancel(later));
        Assert.Equal(5L, registration.Version);
        Assert.Equal(RegistrationSample.T4, registration.ApprovedAt);
    }

    [Fact]
    public void Transition_timestamps_are_utc_and_monotonic()
    {
        var registration = RegistrationSample.New();
        registration.StartProcessing(RegistrationSample.T1);
        Assert.Equal(DomainErrorCodes.InvalidTimestamp,
            Assert.Throws<DomainException>(() => registration.RequestFraudCheck(RegistrationSample.T0)).Code);
        Assert.Equal(DomainErrorCodes.InvalidTimestamp,
            Assert.Throws<DomainException>(() => registration.RequestFraudCheck(
                new DateTimeOffset(2026, 10, 12, 10, 2, 0, TimeSpan.FromHours(-2)))).Code);
        Assert.Equal(FraudCheckStatus.NotRequested, registration.FraudStatus);
        Assert.Equal(RegistrationSample.T1, registration.UpdatedAt);
        Assert.Equal(2L, registration.Version);
    }

    [Fact]
    public void Clearing_transient_events_does_not_change_version()
    {
        var registration = RegistrationSample.New();
        registration.ClearDomainEvents();
        registration.StartProcessing(RegistrationSample.T1);

        Assert.IsType<RegistrationProcessingStarted>(Assert.Single(registration.DomainEvents));
        Assert.Equal(2L, registration.Version);
    }
}
