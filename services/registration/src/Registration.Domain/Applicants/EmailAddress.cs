namespace Registration.Domain.Applicants;

public sealed record EmailAddress
{
    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static EmailAddress Create(string? value)
    {
        if (value is null)
            throw Invalid();

        var trimmed = value.Trim();
        var at = trimmed.IndexOf('@');
        if (trimmed.Length > 254 || at is < 1 or > 64 || at != trimmed.LastIndexOf('@') ||
            trimmed.Any(char.IsWhiteSpace) || trimmed.Any(char.IsControl))
            throw Invalid();

        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];
        if (local.StartsWith('.') || local.EndsWith('.') || local.Contains("..", StringComparison.Ordinal) ||
            domain.Length is 0 or > 253 || !domain.Contains('.') ||
            domain.Split('.').Any(label => label.Length is 0 or > 63 || label.StartsWith('-') || label.EndsWith('-') ||
                label.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-'))))
            throw Invalid();

        return new EmailAddress($"{local}@{domain.ToLowerInvariant()}");
    }

    private static DomainException Invalid() =>
        new(DomainErrorCodes.InvalidEmail, "Email address has an invalid format.");

    public override string ToString() => Value;
}
