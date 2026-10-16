using System.Globalization;

namespace Registration.Domain.Applicants;

public sealed record MonthlyIncome
{
    private MonthlyIncome(decimal value) => Value = value;

    public decimal Value { get; }

    public static MonthlyIncome Create(decimal value) => value < 0
        ? throw new DomainException(DomainErrorCodes.InvalidMonthlyIncome, "Monthly income cannot be negative.")
        : new MonthlyIncome(value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
