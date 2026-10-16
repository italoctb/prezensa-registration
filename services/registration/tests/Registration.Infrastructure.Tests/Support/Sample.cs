using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;
using Registration.Domain.Drafts;
using Registration.Domain.Registrations;
using RegistrationAggregate = Registration.Domain.Registrations.Registration;

namespace Registration.Infrastructure.Tests.Support;

internal static class Sample
{
    public static readonly DateTimeOffset T0 = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset T1 = T0.AddMinutes(1);
    public static readonly DateTimeOffset T2 = T0.AddMinutes(2);

    public static RegistrationDraft Draft(OwnerId? ownerId = null) =>
        RegistrationDraft.Start(RegistrationDraftId.From(Guid.NewGuid()),
            ownerId ?? OwnerId.From(Guid.NewGuid()), T0);

    public static ApplicantProfile Applicant(Cpf? cpf = null) => ApplicantProfile.Create(
        FullName.Create("Ana Maria"), cpf ?? Cpf.Create("52998224725"),
        BirthDate.Create(new DateOnly(1990, 5, 20)), EmailAddress.Create("ana@example.com"),
        PhoneNumber.Create("11912345678"), MotherName.Create("Maria Silva"),
        Nationality.Create("Brasileira"),
        Address.Create(PostalCode.Create("01001000"), Street.Create("Praça da Sé"),
            District.Create("Sé"), City.Create("São Paulo"), FederativeUnit.Create("SP"),
            AddressNumber.Create("10")),
        Profession.Create("Engenheira"), MonthlyIncome.Create(5000m));

    public static RegistrationAggregate Registration(Cpf? cpf = null) =>
        RegistrationAggregate.Create(RegistrationId.From(Guid.NewGuid()), Applicant(cpf), T0);

    public static void Complete(RegistrationDraft draft)
    {
        var profile = Applicant();
        draft.SetFullName(profile.FullName, T1);
        draft.SetCpf(profile.Cpf, T1);
        draft.SetBirthDate(profile.BirthDate, T1);
        draft.SetEmail(profile.Email, T1);
        draft.SetPhone(profile.Phone, T1);
        draft.SetMotherName(profile.MotherName, T1);
        draft.SetNationality(profile.Nationality, T1);
        draft.SetAddress(profile.Address, T1);
        draft.SetProfession(profile.Profession, T1);
        draft.SetMonthlyIncome(profile.MonthlyIncome, T1);
    }
}
