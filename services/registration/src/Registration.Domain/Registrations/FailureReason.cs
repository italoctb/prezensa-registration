namespace Registration.Domain.Registrations;

public sealed record FailureReason
{
    private FailureReason(string value) => Value = value;

    public string Value { get; }

    public static FailureReason Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 500 || value.Any(char.IsControl))
            throw new DomainException(DomainErrorCodes.InvalidFailureReason, "Failure reason must contain 1 to 500 non-control characters.");

        return new FailureReason(value.Trim());
    }

    public override string ToString() => Value;
}
