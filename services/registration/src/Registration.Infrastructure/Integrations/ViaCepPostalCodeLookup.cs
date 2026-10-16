using System.Text.Json;
using Registration.Application.Abstractions.Integrations;
using Registration.Domain.Applicants.Addresses;

namespace Registration.Infrastructure.Integrations;

public sealed class ViaCepPostalCodeLookup(HttpClient client) : IPostalCodeLookup
{
    public async Task<PostalCodeDetails?> LookupAsync(PostalCode postalCode, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"ws/{postalCode.Digits}/json/",
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        if (root.TryGetProperty("erro", out var error) && error.ValueKind == JsonValueKind.True)
            return null;

        if (!Required(root, "logradouro", out var street) ||
            !Required(root, "bairro", out var district) ||
            !Required(root, "localidade", out var city) ||
            !Required(root, "uf", out var state))
            return null;

        return new PostalCodeDetails(postalCode, Street.Create(street), District.Create(district),
            City.Create(city), FederativeUnit.Create(state));
    }

    private static bool Required(JsonElement root, string name, out string value)
    {
        value = root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()?.Trim() ?? string.Empty
            : string.Empty;
        return value.Length > 0;
    }
}
