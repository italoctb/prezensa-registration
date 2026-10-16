using Registration.Application.Accounts;
using Registration.Infrastructure.Integrations;

namespace Registration.Infrastructure.Tests.Integrations;

public sealed class SmtpAuthenticationEmailSenderTests
{
    [Fact]
    public void Account_code_message_has_corporate_recipient_and_expiration()
    {
        var settings = new SmtpAuthenticationSettings("smtp.example.test", 587,
            "no-reply@prezensa.com", null, null);
        using var message = SmtpAuthenticationEmailSender.Compose(settings,
            CorporateEmail.Create("ANA@PREZENSA.COM"), EmailCodePurpose.CreateAccount, "12345678");

        Assert.Equal("ana@prezensa.com", Assert.Single(message.To).Address);
        Assert.NotNull(message.From);
        Assert.Equal(settings.From, message.From.Address);
        Assert.Contains("12345678", message.Body);
        Assert.Contains("10 minutos", message.Body);
        Assert.Contains("criar sua conta", message.Subject);
    }

    [Fact]
    public void Login_message_has_a_distinct_purpose()
    {
        var settings = new SmtpAuthenticationSettings("smtp.example.test", 587,
            "no-reply@prezensa.com", null, null);
        using var message = SmtpAuthenticationEmailSender.Compose(settings,
            CorporateEmail.Create("ana@prezensa.com"), EmailCodePurpose.Login, "87654321");

        Assert.Contains("entrar", message.Subject);
        Assert.Contains("87654321", message.Body);
    }
}
