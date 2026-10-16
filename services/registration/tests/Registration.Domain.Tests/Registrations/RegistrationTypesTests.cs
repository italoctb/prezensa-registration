using Registration.Domain.Registrations;

namespace Registration.Domain.Tests.Registrations;

public sealed class RegistrationTypesTests
{
    [Fact]
    public void Registration_id_rejects_empty_and_compares_by_value()
    {
        Assert.Equal(DomainErrorCodes.InvalidId,
            Assert.Throws<DomainException>(() => RegistrationId.From(Guid.Empty)).Code);
        var value = Guid.NewGuid();
        Assert.Equal(RegistrationId.From(value), RegistrationId.From(value));
        Assert.NotEqual(RegistrationId.From(value), RegistrationId.From(Guid.NewGuid()));
    }

    [Fact]
    public void Approval_actor_requires_a_nonempty_identifier()
    {
        Assert.Equal("registration-system", ApprovalActor.Create(" registration-system ").Value);
        Assert.Equal(ApprovalActor.Create("system"), ApprovalActor.Create(" system "));
        Assert.Equal(DomainErrorCodes.InvalidApprovalActor,
            Assert.Throws<DomainException>(() => ApprovalActor.Create(" ")).Code);
    }

    [Fact]
    public void Failure_reason_is_bounded_and_compares_by_value()
    {
        Assert.Equal(FailureReason.Create("Timeout"), FailureReason.Create(" Timeout "));
        Assert.Equal(DomainErrorCodes.InvalidFailureReason,
            Assert.Throws<DomainException>(() => FailureReason.Create(" ")).Code);
        Assert.Equal(DomainErrorCodes.InvalidFailureReason,
            Assert.Throws<DomainException>(() => FailureReason.Create(new string('A', 501))).Code);
    }
}
