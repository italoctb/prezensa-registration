namespace Registration.Domain.Drafts;

public readonly record struct OwnerId
{
    private OwnerId(Guid value) => Value = value;
    public Guid Value { get; }
    public static OwnerId From(Guid value) => value == Guid.Empty
        ? throw new DomainException(DomainErrorCodes.InvalidId, "Owner ID cannot be empty.")
        : new OwnerId(value);
    public override string ToString() => Value.ToString();
}
