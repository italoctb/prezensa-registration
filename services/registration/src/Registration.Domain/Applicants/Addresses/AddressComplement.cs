namespace Registration.Domain.Applicants.Addresses;

public sealed record AddressComplement
{
    private AddressComplement(string value) => Value = value;
    public string Value { get; }
    public static AddressComplement Create(string? value) => new(AddressText.Required(value, "complement"));
    public override string ToString() => Value;
}
