using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;

namespace Registration.Domain.Tests.Applicants;

public sealed class ApplicantProfileTests
{
    [Fact]
    public void Complete_profile_preserves_its_typed_values()
    {
        var profile = Registrations.RegistrationSample.Applicant();
        Assert.Equal("Ana Maria", profile.FullName.Value);
        Assert.Equal("52998224725", profile.Cpf.Digits);
        Assert.Equal(new DateOnly(1990, 5, 20), profile.BirthDate.Value);
        Assert.Equal("ana@example.com", profile.Email.Value);
        Assert.Equal("11912345678", profile.Phone.Digits);
        Assert.Equal("Maria Silva", profile.MotherName.Value);
        Assert.Equal("Brasileira", profile.Nationality.Value);
        Assert.Equal("12345678", profile.Address.PostalCode.Digits);
        Assert.Equal("Engenheira", profile.Profession.Value);
        Assert.Equal(5000m, profile.MonthlyIncome.Value);
    }

    [Theory]
    [InlineData("name", DomainErrorCodes.InvalidName)]
    [InlineData("cpf", DomainErrorCodes.InvalidCpf)]
    [InlineData("birth", DomainErrorCodes.InvalidBirthDate)]
    [InlineData("email", DomainErrorCodes.InvalidEmail)]
    [InlineData("phone", DomainErrorCodes.InvalidPhone)]
    [InlineData("mother", DomainErrorCodes.InvalidMotherName)]
    [InlineData("nationality", DomainErrorCodes.InvalidNationality)]
    [InlineData("address", DomainErrorCodes.InvalidAddress)]
    [InlineData("profession", DomainErrorCodes.InvalidProfession)]
    [InlineData("income", DomainErrorCodes.InvalidMonthlyIncome)]
    public void Every_field_is_required(string missing, string errorCode)
    {
        var valid = Registrations.RegistrationSample.Applicant();
        FullName? name = missing == "name" ? null : valid.FullName;
        Cpf? cpf = missing == "cpf" ? null : valid.Cpf;
        BirthDate? birth = missing == "birth" ? null : valid.BirthDate;
        EmailAddress? email = missing == "email" ? null : valid.Email;
        PhoneNumber? phone = missing == "phone" ? null : valid.Phone;
        MotherName? mother = missing == "mother" ? null : valid.MotherName;
        Nationality? nationality = missing == "nationality" ? null : valid.Nationality;
        Address? address = missing == "address" ? null : valid.Address;
        Profession? profession = missing == "profession" ? null : valid.Profession;
        MonthlyIncome? income = missing == "income" ? null : valid.MonthlyIncome;

        Assert.Equal(errorCode, Assert.Throws<DomainException>(() => ApplicantProfile.Create(
            name, cpf, birth, email, phone, mother, nationality, address, profession, income)).Code);
    }
}
