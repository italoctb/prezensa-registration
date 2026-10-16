using System.Globalization;
using MongoDB.Bson;
using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;
using Registration.Domain.Drafts;
using Registration.Domain.Registrations;

namespace Registration.Infrastructure.Persistence.Mongo;

internal static class MongoDraftDocumentMapper
{
    public static BsonDocument ToDocument(RegistrationDraft draft) => new()
    {
        ["_id"] = draft.Id.Value.ToString("D"),
        ["ownerId"] = draft.OwnerId.Value.ToString("D"),
        ["status"] = draft.Status.ToString(),
        ["createdAtTicks"] = draft.CreatedAt.UtcTicks,
        ["updatedAtTicks"] = draft.UpdatedAt.UtcTicks,
        ["submittedAtTicks"] = Ticks(draft.SubmittedAt),
        ["registrationId"] = Text(draft.RegistrationId?.Value.ToString("D")),
        ["version"] = draft.Version,
        ["fullName"] = Text(draft.FullName?.Value),
        ["cpf"] = Text(draft.Cpf?.Digits),
        ["birthDate"] = Text(draft.BirthDate?.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        ["email"] = Text(draft.Email?.Value),
        ["phone"] = Text(draft.Phone?.Digits),
        ["motherName"] = Text(draft.MotherName?.Value),
        ["nationality"] = Text(draft.Nationality?.Value),
        ["address"] = draft.Address is null ? BsonNull.Value : AddressDocument(draft.Address),
        ["profession"] = Text(draft.Profession?.Value),
        ["monthlyIncome"] = Text(draft.MonthlyIncome?.Value.ToString(CultureInfo.InvariantCulture))
    };

    public static RegistrationDraft ToDomain(BsonDocument document)
    {
        var submittedTicks = LongOrNull(document, "submittedAtTicks");
        var registrationId = StringOrNull(document, "registrationId");
        var birthDate = StringOrNull(document, "birthDate");
        var income = StringOrNull(document, "monthlyIncome");
        return RegistrationDraft.Restore(new RegistrationDraftState(
            RegistrationDraftId.From(Guid.Parse(document["_id"].AsString)),
            OwnerId.From(Guid.Parse(document["ownerId"].AsString)),
            Enum.Parse<RegistrationDraftStatus>(document["status"].AsString),
            Utc(document["createdAtTicks"].AsInt64),
            Utc(document["updatedAtTicks"].AsInt64),
            submittedTicks is null ? null : Utc(submittedTicks.Value),
            registrationId is null ? null : RegistrationId.From(Guid.Parse(registrationId)),
            document["version"].AsInt64,
            Make(document, "fullName", FullName.Create),
            Make(document, "cpf", Cpf.Create),
            birthDate is null ? null : BirthDate.Create(DateOnly.ParseExact(birthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture)),
            Make(document, "email", EmailAddress.Create),
            Make(document, "phone", PhoneNumber.Create),
            Make(document, "motherName", MotherName.Create),
            Make(document, "nationality", Nationality.Create),
            document.TryGetValue("address", out var address) && !address.IsBsonNull ? ReadAddress(address.AsBsonDocument) : null,
            Make(document, "profession", Profession.Create),
            income is null ? null : MonthlyIncome.Create(decimal.Parse(income, CultureInfo.InvariantCulture))));
    }

    private static BsonDocument AddressDocument(Address address) => new()
    {
        ["postalCode"] = address.PostalCode.Digits,
        ["street"] = address.Street.Value,
        ["district"] = address.District.Value,
        ["city"] = address.City.Value,
        ["federativeUnit"] = address.FederativeUnit.Code,
        ["number"] = address.Number.Value,
        ["complement"] = Text(address.Complement?.Value)
    };

    private static Address ReadAddress(BsonDocument document) => Address.Create(
        PostalCode.Create(document["postalCode"].AsString), Street.Create(document["street"].AsString),
        District.Create(document["district"].AsString), City.Create(document["city"].AsString),
        FederativeUnit.Create(document["federativeUnit"].AsString), AddressNumber.Create(document["number"].AsString),
        Make(document, "complement", AddressComplement.Create));

    private static T? Make<T>(BsonDocument document, string key, Func<string?, T> factory) where T : class =>
        StringOrNull(document, key) is { } text ? factory(text) : null;

    private static string? StringOrNull(BsonDocument document, string key) =>
        document.TryGetValue(key, out var value) && !value.IsBsonNull ? value.AsString : null;

    private static long? LongOrNull(BsonDocument document, string key) =>
        document.TryGetValue(key, out var value) && !value.IsBsonNull ? value.AsInt64 : null;

    private static BsonValue Text(string? value) => value is null ? BsonNull.Value : new BsonString(value);
    private static BsonValue Ticks(DateTimeOffset? value) => value is null ? BsonNull.Value : new BsonInt64(value.Value.UtcTicks);
    private static DateTimeOffset Utc(long ticks) => new(ticks, TimeSpan.Zero);
}
