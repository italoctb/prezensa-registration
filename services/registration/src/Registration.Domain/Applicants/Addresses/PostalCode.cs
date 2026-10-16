namespace Registration.Domain.Applicants.Addresses;

public sealed record PostalCode
{
    private PostalCode(string digits) => Digits = digits;
    public string Digits { get; }

    public static PostalCode Create(string? value)
    {
        if (value is null || value.Any(c => c is not (>= '0' and <= '9') and not (' ' or '-')))
            throw Invalid();
        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length != 8)
            throw Invalid();
        return new PostalCode(digits);
    }

    public override string ToString() => Digits;
    private static DomainException Invalid() => new(DomainErrorCodes.InvalidAddress, "CEP must contain eight digits.");
}
