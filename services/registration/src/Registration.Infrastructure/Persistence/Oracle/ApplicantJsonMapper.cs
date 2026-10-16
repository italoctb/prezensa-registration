using System.Globalization;
using System.Text.Json;
using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;

namespace Registration.Infrastructure.Persistence.Oracle;

internal static class ApplicantJsonMapper
{
    private sealed record AddressData(string PostalCode, string Street, string District, string City,
        string FederativeUnit, string Number, string? Complement);

    private sealed record ApplicantData(string FullName, string Cpf, string BirthDate, string Email,
        string Phone, string MotherName, string Nationality, AddressData Address, string Profession,
        decimal MonthlyIncome);

    public static string ToJson(ApplicantProfile profile) => JsonSerializer.Serialize(new ApplicantData(
        profile.FullName.Value, profile.Cpf.Digits,
        profile.BirthDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), profile.Email.Value,
        profile.Phone.Digits, profile.MotherName.Value, profile.Nationality.Value,
        new AddressData(profile.Address.PostalCode.Digits, profile.Address.Street.Value,
            profile.Address.District.Value, profile.Address.City.Value, profile.Address.FederativeUnit.Code,
            profile.Address.Number.Value, profile.Address.Complement?.Value),
        profile.Profession.Value, profile.MonthlyIncome.Value));

    public static ApplicantProfile ToDomain(string json)
    {
        var data = JsonSerializer.Deserialize<ApplicantData>(json)
            ?? throw new InvalidDataException("Applicant document is missing.");
        return ApplicantProfile.Create(
            FullName.Create(data.FullName), Cpf.Create(data.Cpf),
            BirthDate.Create(DateOnly.ParseExact(data.BirthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture)),
            EmailAddress.Create(data.Email), PhoneNumber.Create(data.Phone),
            MotherName.Create(data.MotherName), Nationality.Create(data.Nationality),
            Address.Create(PostalCode.Create(data.Address.PostalCode), Street.Create(data.Address.Street),
                District.Create(data.Address.District), City.Create(data.Address.City),
                FederativeUnit.Create(data.Address.FederativeUnit), AddressNumber.Create(data.Address.Number),
                data.Address.Complement is null ? null : AddressComplement.Create(data.Address.Complement)),
            Profession.Create(data.Profession), MonthlyIncome.Create(data.MonthlyIncome));
    }
}
