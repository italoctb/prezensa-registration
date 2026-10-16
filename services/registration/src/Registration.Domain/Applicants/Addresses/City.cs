namespace Registration.Domain.Applicants.Addresses;

public sealed record City
{
    private City(string value) => Value = value;
    public string Value { get; }
    public static City Create(string? value) => new(AddressText.Required(value, "city"));
    public override string ToString() => Value;
}
