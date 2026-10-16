namespace Registration.Domain.Applicants.Addresses;

public sealed record Street
{
    private Street(string value) => Value = value;
    public string Value { get; }
    public static Street Create(string? value) => new(AddressText.Required(value, "street"));
    public override string ToString() => Value;
}
