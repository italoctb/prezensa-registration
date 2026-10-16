namespace Registration.Domain.Applicants.Addresses;

public sealed record FederativeUnit
{
    private static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
        "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    };

    private FederativeUnit(string code) => Code = code;
    public string Code { get; }

    public static FederativeUnit Create(string? value)
    {
        var code = value?.Trim().ToUpperInvariant();
        return code is not null && Codes.Contains(code)
            ? new FederativeUnit(code)
            : throw AddressText.Invalid("state (UF)");
    }

    public override string ToString() => Code;
}
