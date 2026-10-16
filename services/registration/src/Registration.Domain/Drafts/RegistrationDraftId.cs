namespace Registration.Domain.Drafts;

public readonly record struct RegistrationDraftId
{
    private RegistrationDraftId(Guid value) => Value = value;
    public Guid Value { get; }
    public static RegistrationDraftId From(Guid value) => value == Guid.Empty
        ? throw new DomainException(DomainErrorCodes.InvalidId, "Draft ID cannot be empty.")
        : new RegistrationDraftId(value);
    public override string ToString() => Value.ToString();
}
