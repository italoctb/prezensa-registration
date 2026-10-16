namespace Registration.Domain.Applicants.Addresses;

public sealed record Address
{
    private Address(PostalCode postalCode, Street street, District district, City city,
        FederativeUnit federativeUnit, AddressNumber number, AddressComplement? complement)
    {
        PostalCode = postalCode;
        Street = street;
        District = district;
        City = city;
        FederativeUnit = federativeUnit;
        Number = number;
        Complement = complement;
    }

    public PostalCode PostalCode { get; }
    public Street Street { get; }
    public District District { get; }
    public City City { get; }
    public FederativeUnit FederativeUnit { get; }
    public AddressNumber Number { get; }
    public AddressComplement? Complement { get; }

    public static Address Create(PostalCode? postalCode, Street? street, District? district, City? city,
        FederativeUnit? federativeUnit, AddressNumber? number, AddressComplement? complement = null) =>
        new(postalCode ?? throw Missing(), street ?? throw Missing(), district ?? throw Missing(),
            city ?? throw Missing(), federativeUnit ?? throw Missing(), number ?? throw Missing(), complement);

    private static DomainException Missing() =>
        new(DomainErrorCodes.InvalidAddress, "Address requires CEP, street, district, city, state and number.");
}
