namespace Registration.Domain.Applicants.Addresses;

public sealed record AddressNumber
{
    private AddressNumber(string value) => Value = value;
    public string Value { get; }
    public static AddressNumber Create(string? value) => new(AddressText.Required(value, "number"));
    public override string ToString() => Value;
}
