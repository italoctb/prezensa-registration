namespace Registration.Domain.Applicants;

public sealed record BirthDate
{
    private BirthDate(DateOnly value) => Value = value;

    public DateOnly Value { get; }

    public static BirthDate Create(DateOnly value) => value == default
        ? throw new DomainException(DomainErrorCodes.InvalidBirthDate, "Birth date is required.")
        : new BirthDate(value);

    public bool IsAtLeast18On(DateOnly referenceDate)
    {
        var years = referenceDate.Year - Value.Year;
        if (years != 18)
            return years > 18;

        return referenceDate >= Value.AddYears(18);
    }

    public override string ToString() => Value.ToString("yyyy-MM-dd");
}
