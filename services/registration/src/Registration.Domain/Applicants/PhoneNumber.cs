namespace Registration.Domain.Applicants;

public sealed record PhoneNumber
{
    private PhoneNumber(string digits) => Digits = digits;

    public string Digits { get; }

    public static PhoneNumber Create(string? value)
    {
        if (value is null || value.Any(c => c is not (>= '0' and <= '9') and not (' ' or '(' or ')' or '-')))
            throw Invalid();

        var digits = new string(value.Where(c => c is >= '0' and <= '9').ToArray());
        if (digits.Length is not (10 or 11) || int.Parse(digits[..2]) is < 11 or > 99)
            throw Invalid();

        return new PhoneNumber(digits);
    }

    private static DomainException Invalid() =>
        new(DomainErrorCodes.InvalidPhone, "Phone must contain a two-digit Brazilian area code and 8 or 9 subscriber digits.");

    public override string ToString() => Digits;
}
