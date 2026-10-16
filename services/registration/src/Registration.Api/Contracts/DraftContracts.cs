using Registration.Application.Drafts;
using Registration.Domain.Applicants.Addresses;

namespace Registration.Api.Contracts;

public sealed record UpdateFullNameRequest(string? FullName);
public sealed record UpdateDocumentRequest(string? Document);
public sealed record UpdateBirthDateRequest(DateOnly BirthDate);
public sealed record UpdateEmailRequest(string? Email);
public sealed record UpdatePhoneRequest(string? Phone);
public sealed record UpdateMotherNameRequest(string? MotherName);
public sealed record UpdateNationalityRequest(string? Nationality);
public sealed record UpdateAddressRequest(string? PostalCode, string? Street, string? District,
    string? City, string? FederativeUnit, string? Number, string? Complement);
public sealed record UpdateProfessionRequest(string? Profession);
public sealed record UpdateMonthlyIncomeRequest(decimal? MonthlyIncome);

public sealed record DraftAddressResponse(string PostalCode, string Street, string District,
    string City, string FederativeUnit, string Number, string? Complement)
{
    public static DraftAddressResponse From(Address address) => new(
        address.PostalCode.Digits, address.Street.Value, address.District.Value,
        address.City.Value, address.FederativeUnit.Code, address.Number.Value,
        address.Complement?.Value);
}

public sealed record DraftResponse(Guid DraftId, string Status, string CurrentStep,
    string? FullName, string? Document, DateOnly? BirthDate, string? Email, string? Phone,
    string? MotherName, string? Nationality, DraftAddressResponse? Address,
    string? Profession, decimal? MonthlyIncome, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, DateTimeOffset? SubmittedAt, Guid? RegistrationId, long Version)
{
    public static DraftResponse From(DraftSnapshot draft) => new(
        draft.DraftId.Value, draft.Status.ToString(), draft.CurrentStep.ToString(),
        draft.FullName?.Value, draft.Cpf?.Digits, draft.BirthDate?.Value,
        draft.Email?.Value, draft.Phone?.Digits, draft.MotherName?.Value,
        draft.Nationality?.Value,
        draft.Address is null ? null : DraftAddressResponse.From(draft.Address),
        draft.Profession?.Value, draft.MonthlyIncome?.Value,
        draft.CreatedAt, draft.UpdatedAt, draft.SubmittedAt,
        draft.RegistrationId?.Value, draft.Version);
}

public sealed record PostalCodeResponse(string PostalCode, string Street, string District,
    string City, string FederativeUnit)
{
    public static PostalCodeResponse From(Registration.Application.Abstractions.Integrations.PostalCodeDetails value) =>
        new(value.PostalCode.Digits, value.Street.Value, value.District.Value,
            value.City.Value, value.FederativeUnit.Code);
}

public sealed record SubmitDraftResponse(Guid RegistrationId);
