using Registration.Domain.Applicants.Addresses;

namespace Registration.Application.Abstractions.Integrations;

public sealed record PostalCodeDetails(PostalCode PostalCode, Street Street, District District, City City, FederativeUnit FederativeUnit);

public interface IPostalCodeLookup
{
    Task<PostalCodeDetails?> LookupAsync(PostalCode postalCode, CancellationToken cancellationToken);
}
