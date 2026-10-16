using Registration.Domain.Applicants;

namespace Registration.Domain.Tests.Applicants;

public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("(11) 91234-5678", "11912345678")]
    [InlineData("1132345678", "1132345678")]
    public void Stores_ddd_and_subscriber_as_digits(string input, string expected) =>
        Assert.Equal(expected, PhoneNumber.Create(input).Digits);

    [Theory]
    [InlineData((object?)null)]
    [InlineData("")]
    [InlineData("912345678")]
    [InlineData("0012345678")]
    [InlineData("119123456789")]
    [InlineData("+5511912345678")]
    [InlineData("11 9123A-5678")]
    public void Rejects_missing_ddd_wrong_length_or_non_digits(string? input) =>
        Assert.Equal(DomainErrorCodes.InvalidPhone,
            Assert.Throws<DomainException>(() => PhoneNumber.Create(input)).Code);

    [Fact]
    public void Equality_uses_normalized_digits() =>
        Assert.Equal(PhoneNumber.Create("(11) 91234-5678"), PhoneNumber.Create("11912345678"));
}
