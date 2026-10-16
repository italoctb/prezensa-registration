using Registration.Domain.Applicants;

namespace Registration.Domain.Tests.Applicants;

public sealed class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    public void Valid_cpf_is_stored_as_digits(string value) =>
        Assert.Equal(value.Replace(".", "").Replace("-", ""), Cpf.Create(value).Digits);

    [Theory]
    [InlineData((object?)null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("11111111111")]
    [InlineData("52998224726")]
    [InlineData("5299822472X")]
    public void Invalid_length_sequence_or_check_digits_are_rejected(string? value) =>
        Assert.Equal(DomainErrorCodes.InvalidCpf, Assert.Throws<DomainException>(() => Cpf.Create(value)).Code);

    [Fact]
    public void Equality_uses_canonical_digits() =>
        Assert.Equal(Cpf.Create("529.982.247-25"), Cpf.Create("52998224725"));
}
