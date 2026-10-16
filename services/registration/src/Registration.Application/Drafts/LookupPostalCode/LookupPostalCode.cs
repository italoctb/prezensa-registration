using Registration.Application.Abstractions.Integrations;
using Registration.Domain.Applicants.Addresses;

namespace Registration.Application.Drafts.LookupPostalCode;

public sealed record LookupPostalCodeQuery(string PostalCode);

public sealed class LookupPostalCodeHandler(IPostalCodeLookup lookup)
{
    public async Task<PostalCodeDetails> HandleAsync(LookupPostalCodeQuery query, CancellationToken cancellationToken = default)
    {
        var postalCode = PostalCode.Create(query.PostalCode);
        return await lookup.LookupAsync(postalCode, cancellationToken)
            ?? throw new RegistrationApplicationException(ApplicationErrorCodes.PostalCodeNotFound, "Postal code was not found.");
    }
}
