using System.Globalization;

namespace Registration.Domain.Applicants;

public sealed record FullName
{
    private FullName(string value) => Value = value;

    public string Value { get; }

    public static FullName Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(DomainErrorCodes.InvalidName, "Full name is required.");

        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var length = new StringInfo(normalized).LengthInTextElements;
        if (length is < 3 or > 15 || normalized.Any(char.IsControl))
            throw new DomainException(DomainErrorCodes.InvalidName, "Full name must contain 3 to 15 characters.");

        return new FullName(normalized);
    }

    public override string ToString() => Value;
}
