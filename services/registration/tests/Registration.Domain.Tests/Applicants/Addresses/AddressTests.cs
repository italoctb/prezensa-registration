using Registration.Domain.Applicants.Addresses;

namespace Registration.Domain.Tests.Applicants.Addresses;

public sealed class AddressTests
{
    [Fact]
    public void Normalizes_cep_and_required_text()
    {
        var address = Sample("12345-678", "  Rua  das Flores ", " Centro ", " São  Paulo ", " sp ", " 10 ", " apto  2 ");
        Assert.Equal("12345678", address.PostalCode.Digits);
        Assert.Equal("Rua das Flores", address.Street.Value);
        Assert.Equal("Centro", address.District.Value);
        Assert.Equal("São Paulo", address.City.Value);
        Assert.Equal("SP", address.FederativeUnit.Code);
        Assert.Equal("10", address.Number.Value);
        Assert.Equal("apto 2", address.Complement?.Value);
        Assert.Equal(address, Sample("12345678", "Rua das Flores", "Centro", "São Paulo", "SP", "10", "apto 2"));
    }

    [Theory]
    [InlineData((object?)null)]
    [InlineData("1234567")]
    [InlineData("1234567X")]
    public void Missing_or_malformed_cep_is_rejected(string? cep) =>
        Assert.Equal(DomainErrorCodes.InvalidAddress,
            Assert.Throws<DomainException>(() => PostalCode.Create(cep)).Code);

    [Theory]
    [InlineData("XX")]
    [InlineData("ZZ")]
    [InlineData("")]
    public void Invalid_uf_is_rejected(string uf) =>
        Assert.Equal(DomainErrorCodes.InvalidAddress,
            Assert.Throws<DomainException>(() => FederativeUnit.Create(uf)).Code);

    [Fact]
    public void Required_parts_and_optional_complement_are_enforced()
    {
        Assert.Equal(DomainErrorCodes.InvalidAddress, Assert.Throws<DomainException>(() => Street.Create(null)).Code);
        Assert.Equal(DomainErrorCodes.InvalidAddress, Assert.Throws<DomainException>(() => District.Create(" ")).Code);
        Assert.Equal(DomainErrorCodes.InvalidAddress, Assert.Throws<DomainException>(() => City.Create(null)).Code);
        Assert.Equal(DomainErrorCodes.InvalidAddress, Assert.Throws<DomainException>(() => AddressNumber.Create(" ")).Code);
        Assert.Equal(DomainErrorCodes.InvalidAddress, Assert.Throws<DomainException>(() => AddressComplement.Create(" ")).Code);
        Assert.Equal(DomainErrorCodes.InvalidAddress, Assert.Throws<DomainException>(() =>
            Address.Create(null, Street.Create("Rua"), District.Create("Centro"), City.Create("São Paulo"),
                FederativeUnit.Create("SP"), AddressNumber.Create("10"))).Code);
        Assert.Null(Sample().Complement);
    }

    private static Address Sample(string cep = "12345678", string street = "Rua das Flores", string district = "Centro",
        string city = "São Paulo", string uf = "SP", string number = "10", string? complement = null) =>
        Address.Create(PostalCode.Create(cep), Street.Create(street), District.Create(district),
            City.Create(city), FederativeUnit.Create(uf), AddressNumber.Create(number),
            complement is null ? null : AddressComplement.Create(complement));
}
