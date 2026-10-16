namespace Registration.Domain.Applicants;

public sealed record MotherName
{
    private MotherName(string value) => Value = value;

    public string Value { get; }

    public static MotherName Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(DomainErrorCodes.InvalidMotherName, "Mother's name is required.");

        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Any(char.IsControl))
            throw new DomainException(DomainErrorCodes.InvalidMotherName, "Mother's name contains invalid characters.");

        return new MotherName(normalized);
    }

    public override string ToString() => Value;
}
