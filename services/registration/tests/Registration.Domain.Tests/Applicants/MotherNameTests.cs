using Registration.Domain.Applicants;

namespace Registration.Domain.Tests.Applicants;

public sealed class MotherNameTests
{
    [Fact]
    public void Required_name_normalizes_spaces_without_customers_short_length_rule()
    {
        Assert.Equal("Maria da Silva", MotherName.Create("  Maria  da Silva ").Value);
        Assert.Equal(MotherName.Create("Maria da Silva"), MotherName.Create(" Maria  da Silva "));
    }

    [Theory]
    [InlineData((object?)null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_name_is_rejected(string? value) =>
        Assert.Equal(DomainErrorCodes.InvalidMotherName,
            Assert.Throws<DomainException>(() => MotherName.Create(value)).Code);
}
