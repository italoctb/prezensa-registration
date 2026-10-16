namespace Registration.Domain.Applicants.Addresses;

internal static class AddressText
{
    public static string Required(string? value, string part)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Invalid(part);
        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Any(char.IsControl))
            throw Invalid(part);
        return normalized;
    }

    public static DomainException Invalid(string part) =>
        new(DomainErrorCodes.InvalidAddress, $"Address {part} is required and must be valid.");
}
