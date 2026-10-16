using Registration.Domain.Applicants;

namespace Registration.Domain.Tests.Applicants;

public sealed class NationalityTests
{
    [Fact]
    public void Description_is_normalized_without_a_country_catalog()
    {
        Assert.Equal("Brasileira", Nationality.Create("  Brasileira ").Value);
        Assert.Equal(Nationality.Create("Portuguesa"), Nationality.Create(" Portuguesa "));
    }

    [Theory]
    [InlineData((object?)null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Missing_nationality_is_rejected(string? value) =>
        Assert.Equal(DomainErrorCodes.InvalidNationality,
            Assert.Throws<DomainException>(() => Nationality.Create(value)).Code);
}
