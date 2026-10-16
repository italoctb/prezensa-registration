using Registration.Domain.Applicants;

namespace Registration.Domain.Tests.Applicants;

public sealed class MonthlyIncomeTests
{
    [Fact]
    public void Zero_and_positive_amounts_are_valid()
    {
        Assert.Equal(0m, MonthlyIncome.Create(0m).Value);
        Assert.Equal(1500.75m, MonthlyIncome.Create(1500.75m).Value);
    }

    [Fact]
    public void Negative_amount_is_rejected() =>
        Assert.Equal(DomainErrorCodes.InvalidMonthlyIncome,
            Assert.Throws<DomainException>(() => MonthlyIncome.Create(-0.01m)).Code);

    [Fact]
    public void Equality_is_by_decimal_value() =>
        Assert.Equal(MonthlyIncome.Create(100m), MonthlyIncome.Create(100.00m));
}
