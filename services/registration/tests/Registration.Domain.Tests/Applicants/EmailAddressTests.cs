using Registration.Domain.Applicants;

namespace Registration.Domain.Tests.Applicants;

public sealed class EmailAddressTests
{
    [Fact]
    public void Trims_and_lowercases_only_domain()
    {
        Assert.Equal("Ana@exemplo.com", EmailAddress.Create(" Ana@EXEMPLO.COM ").Value);
        Assert.Equal(EmailAddress.Create("Ana@EXEMPLO.COM"), EmailAddress.Create("Ana@exemplo.com"));
        Assert.NotEqual(EmailAddress.Create("Ana@exemplo.com"), EmailAddress.Create("ana@exemplo.com"));
    }

    [Theory]
    [InlineData((object?)null)]
    [InlineData("")]
    [InlineData("ana")]
    [InlineData("a@@example.com")]
    [InlineData("a@example")]
    [InlineData("a@-example.com")]
    [InlineData("a b@example.com")]
    public void Rejects_pragmatically_invalid_format(string? value) =>
        Assert.Equal(DomainErrorCodes.InvalidEmail,
            Assert.Throws<DomainException>(() => EmailAddress.Create(value)).Code);
}
