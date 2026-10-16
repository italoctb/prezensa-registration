namespace Registration.Domain.Applicants.Addresses;

public sealed record District
{
    private District(string value) => Value = value;
    public string Value { get; }
    public static District Create(string? value) => new(AddressText.Required(value, "district"));
    public override string ToString() => Value;
}
