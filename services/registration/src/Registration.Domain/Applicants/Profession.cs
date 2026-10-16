namespace Registration.Domain.Applicants;

public sealed record Profession
{
    private Profession(string value) => Value = value;

    public string Value { get; }

    public static Profession Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(DomainErrorCodes.InvalidProfession, "Profession is required.");

        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Any(char.IsControl))
            throw new DomainException(DomainErrorCodes.InvalidProfession, "Profession contains invalid characters.");

        return new Profession(normalized);
    }

    public override string ToString() => Value;
}
