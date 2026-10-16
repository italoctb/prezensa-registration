using Registration.Domain.Registrations;
using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;
using RegistrationAggregate = global::Registration.Domain.Registrations.Registration;

namespace Registration.Domain.Tests.Registrations;

internal static class RegistrationSample
{
    public static readonly DateTimeOffset T0 = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset T1 = T0.AddMinutes(1);
    public static readonly DateTimeOffset T2 = T0.AddMinutes(2);
    public static readonly DateTimeOffset T3 = T0.AddMinutes(3);
    public static readonly DateTimeOffset T4 = T0.AddMinutes(4);

    public static ApplicantProfile Applicant(DateOnly? birthDate = null) => ApplicantProfile.Create(
        FullName.Create("Ana Maria"),
        Cpf.Create("52998224725"),
        BirthDate.Create(birthDate ?? new DateOnly(1990, 5, 20)),
        EmailAddress.Create("ana@example.com"),
        PhoneNumber.Create("11912345678"),
        MotherName.Create("Maria Silva"),
        Nationality.Create("Brasileira"),
        Address.Create(PostalCode.Create("12345678"), Street.Create("Rua das Flores"),
            District.Create("Centro"), City.Create("São Paulo"), FederativeUnit.Create("SP"),
            AddressNumber.Create("10")),
        Profession.Create("Engenheira"),
        MonthlyIncome.Create(5000m));

    public static RegistrationAggregate New(RegistrationId? id = null, ApplicantProfile? applicant = null) =>
        RegistrationAggregate.Create(id ?? RegistrationId.From(Guid.NewGuid()), applicant ?? Applicant(), T0);

    public static RegistrationAggregate PendingFraud()
    {
        var registration = New();
        registration.StartProcessing(T1);
        registration.RequestFraudCheck(T2);
        return registration;
    }

    public static ApprovalActor Approver() => ApprovalActor.Create("registration-system");

    public static void AssertInvalidTransition(Action action) =>
        Assert.Equal(DomainErrorCodes.InvalidTransition, Assert.Throws<DomainException>(action).Code);
}
