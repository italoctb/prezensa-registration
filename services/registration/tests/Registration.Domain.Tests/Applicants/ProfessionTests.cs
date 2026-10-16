using Registration.Domain.Applicants;

namespace Registration.Domain.Tests.Applicants;

public sealed class ProfessionTests
{
    [Fact]
    public void Normalizes_description_without_profession_enum()
    {
        Assert.Equal("Engenheira de Software", Profession.Create(" Engenheira  de Software ").Value);
        Assert.Equal(Profession.Create("Engenheira"), Profession.Create(" Engenheira "));
    }

    [Theory]
    [InlineData((object?)null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Missing_profession_is_rejected(string? value) =>
        Assert.Equal(DomainErrorCodes.InvalidProfession,
            Assert.Throws<DomainException>(() => Profession.Create(value)).Code);
}
