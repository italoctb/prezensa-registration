using Registration.Domain.Applicants;

namespace Registration.Application.Accounts;

public sealed record CorporateEmail
{
    private CorporateEmail(string value) => Value = value;
    public string Value { get; }

    public static CorporateEmail Create(string? value)
    {
        EmailAddress email;
        try { email = EmailAddress.Create(value); }
        catch (Registration.Domain.DomainException)
        {
            throw Invalid();
        }

        var at = email.Value.LastIndexOf('@');
        var local = email.Value[..at];
        if (!email.Value[(at + 1)..].Equals("prezensa.com", StringComparison.Ordinal) ||
            local.Length is < 1 or > 64 ||
            local.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '%' or '+' or '-')))
            throw Invalid();

        return new CorporateEmail($"{local.ToLowerInvariant()}@prezensa.com");
    }

    private static RegistrationApplicationException Invalid() => new(
        AccountErrorCodes.InvalidCorporateEmail, "A valid @prezensa.com email is required.");

    public override string ToString() => Value;
}
