namespace Registration.Domain.Applicants;

public sealed record Cpf
{
    private Cpf(string digits) => Digits = digits;

    public string Digits { get; }

    public static Cpf Create(string? value)
    {
        if (value is null)
            throw Invalid();

        var digits = new string(value.Where(c => c is not ('.' or '-' or ' ')).ToArray());
        if (digits.Length != 11 || digits.Any(c => c is < '0' or > '9') || digits.All(c => c == digits[0]))
            throw Invalid();

        if (CalculateCheckDigit(digits.AsSpan(0, 9), 10) != digits[9] - '0' ||
            CalculateCheckDigit(digits.AsSpan(0, 10), 11) != digits[10] - '0')
            throw Invalid();

        return new Cpf(digits);
    }

    private static int CalculateCheckDigit(ReadOnlySpan<char> digits, int weight)
    {
        var sum = 0;
        foreach (var digit in digits)
            sum += (digit - '0') * weight--;

        var remainder = (sum * 10) % 11;
        return remainder == 10 ? 0 : remainder;
    }

    private static DomainException Invalid() =>
        new(DomainErrorCodes.InvalidCpf, "CPF must contain 11 valid digits.");

    public override string ToString() => Digits;
}
