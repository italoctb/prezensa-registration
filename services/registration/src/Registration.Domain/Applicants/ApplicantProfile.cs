using Registration.Domain.Applicants.Addresses;

namespace Registration.Domain.Applicants;

public sealed record ApplicantProfile
{
    private ApplicantProfile(
        FullName fullName,
        Cpf cpf,
        BirthDate birthDate,
        EmailAddress email,
        PhoneNumber phone,
        MotherName motherName,
        Nationality nationality,
        Address address,
        Profession profession,
        MonthlyIncome monthlyIncome)
    {
        FullName = fullName;
        Cpf = cpf;
        BirthDate = birthDate;
        Email = email;
        Phone = phone;
        MotherName = motherName;
        Nationality = nationality;
        Address = address;
        Profession = profession;
        MonthlyIncome = monthlyIncome;
    }

    public FullName FullName { get; }
    public Cpf Cpf { get; }
    public BirthDate BirthDate { get; }
    public EmailAddress Email { get; }
    public PhoneNumber Phone { get; }
    public MotherName MotherName { get; }
    public Nationality Nationality { get; }
    public Address Address { get; }
    public Profession Profession { get; }
    public MonthlyIncome MonthlyIncome { get; }

    public static ApplicantProfile Create(
        FullName? fullName,
        Cpf? cpf,
        BirthDate? birthDate,
        EmailAddress? email,
        PhoneNumber? phone,
        MotherName? motherName,
        Nationality? nationality,
        Address? address,
        Profession? profession,
        MonthlyIncome? monthlyIncome)
    {
        return new ApplicantProfile(
            fullName ?? throw new DomainException(DomainErrorCodes.InvalidName, "Full name is required."),
            cpf ?? throw new DomainException(DomainErrorCodes.InvalidCpf, "CPF is required."),
            birthDate ?? throw new DomainException(DomainErrorCodes.InvalidBirthDate, "Birth date is required."),
            email ?? throw new DomainException(DomainErrorCodes.InvalidEmail, "Email address is required."),
            phone ?? throw new DomainException(DomainErrorCodes.InvalidPhone, "Phone is required."),
            motherName ?? throw new DomainException(DomainErrorCodes.InvalidMotherName, "Mother's name is required."),
            nationality ?? throw new DomainException(DomainErrorCodes.InvalidNationality, "Nationality is required."),
            address ?? throw new DomainException(DomainErrorCodes.InvalidAddress, "Address is required."),
            profession ?? throw new DomainException(DomainErrorCodes.InvalidProfession, "Profession is required."),
            monthlyIncome ?? throw new DomainException(DomainErrorCodes.InvalidMonthlyIncome, "Monthly income is required."));
    }
}
