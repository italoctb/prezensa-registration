using System.Net;
using System.Net.Mail;
using System.Text;
using Registration.Application.Abstractions.Integrations;
using Registration.Application.Accounts;

namespace Registration.Infrastructure.Integrations;

public sealed record SmtpAuthenticationSettings(
    string Host, int Port, string From, string? Username, string? Password);

public sealed class SmtpAuthenticationEmailSender(SmtpAuthenticationSettings settings)
    : IAuthenticationEmailSender
{
    public async Task SendCodeAsync(CorporateEmail email, EmailCodePurpose purpose, string code,
        CancellationToken cancellationToken)
    {
        using var message = Compose(settings, email, purpose, code);

        using var client = new SmtpClient();
        client.Host = settings.Host;
        client.Port = settings.Port;
        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        client.EnableSsl = true;
        client.UseDefaultCredentials = false;
        client.Timeout = 10_000;
        if (settings.Username is not null)
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        await client.SendMailAsync(message, cancellationToken);
    }

    internal static MailMessage Compose(SmtpAuthenticationSettings settings,
        CorporateEmail email, EmailCodePurpose purpose, string code) =>
        new(new MailAddress(settings.From),
            new MailAddress(email.Value))
        {
            Subject = purpose == EmailCodePurpose.CreateAccount
                ? "Código para criar sua conta Prezensa" : "Código para entrar na Prezensa",
            Body = $"Seu código é {code}. Ele expira em 10 minutos e pode ser usado uma única vez. " +
                   "Se você não solicitou este código, ignore esta mensagem.",
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };
}
