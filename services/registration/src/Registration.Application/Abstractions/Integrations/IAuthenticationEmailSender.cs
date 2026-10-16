using Registration.Application.Accounts;

namespace Registration.Application.Abstractions.Integrations;

public interface IAuthenticationEmailSender
{
    Task SendCodeAsync(CorporateEmail email, EmailCodePurpose purpose, string code,
        CancellationToken cancellationToken);
}
