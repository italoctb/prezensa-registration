namespace Registration.Domain.Registrations;

public readonly record struct RegistrationId
{
    private RegistrationId(Guid value) => Value = value;

    public Guid Value { get; }

    public static RegistrationId From(Guid value) => value == Guid.Empty
        ? throw new DomainException(DomainErrorCodes.InvalidId, "Registration ID cannot be empty.")
        : new RegistrationId(value);

    public override string ToString() => Value.ToString();
}
