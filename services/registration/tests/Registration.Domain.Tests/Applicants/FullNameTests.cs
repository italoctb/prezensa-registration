using Registration.Domain.Applicants;

namespace Registration.Domain.Tests.Applicants;

public sealed class FullNameTests
{
    [Theory]
    [InlineData("Ana")]
    [InlineData("123456789012345")]
    public void Accepts_inclusive_length_boundaries(string value) =>
        Assert.Equal(value, FullName.Create(value).Value);

    [Theory]
    [InlineData((object?)null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Al")]
    [InlineData("1234567890123456")]
    public void Rejects_missing_or_out_of_range_name(string? value) =>
        Assert.Equal(DomainErrorCodes.InvalidName, Assert.Throws<DomainException>(() => FullName.Create(value)).Code);

    [Fact]
    public void Normalizes_whitespace_and_compares_by_value()
    {
        Assert.Equal("Ana Maria", FullName.Create("  Ana\t Maria  ").Value);
        Assert.Equal(FullName.Create("Ana Maria"), FullName.Create(" Ana  Maria "));
    }
}
