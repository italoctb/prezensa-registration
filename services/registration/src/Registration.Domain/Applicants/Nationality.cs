namespace Registration.Domain.Applicants;

public sealed record Nationality
{
    private Nationality(string value) => Value = value;

    public string Value { get; }

    public static Nationality Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(DomainErrorCodes.InvalidNationality, "Nationality is required.");

        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Any(char.IsControl))
            throw new DomainException(DomainErrorCodes.InvalidNationality, "Nationality contains invalid characters.");

        return new Nationality(normalized);
    }

    public override string ToString() => Value;
}
