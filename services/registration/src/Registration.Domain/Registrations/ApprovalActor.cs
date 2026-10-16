namespace Registration.Domain.Registrations;

public sealed record ApprovalActor
{
    private ApprovalActor(string value) => Value = value;

    public string Value { get; }

    public static ApprovalActor Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(DomainErrorCodes.InvalidApprovalActor, "Approval actor is required.");

        var normalized = value.Trim();
        if (normalized.Any(char.IsControl))
            throw new DomainException(DomainErrorCodes.InvalidApprovalActor, "Approval actor contains invalid characters.");

        return new ApprovalActor(normalized);
    }

    public override string ToString() => Value;
}
