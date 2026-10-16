using Registration.Domain.Applicants;

namespace Registration.Domain.Tests.Applicants;

public sealed class BirthDateTests
{
    private static readonly DateOnly Today = new(2026, 10, 12);

    [Fact]
    public void Exactly_eighteen_on_birthday_is_adult() =>
        Assert.True(BirthDate.Create(new DateOnly(2008, 10, 12)).IsAtLeast18On(Today));

    [Fact]
    public void Birthday_tomorrow_is_still_underage() =>
        Assert.False(BirthDate.Create(new DateOnly(2008, 10, 13)).IsAtLeast18On(Today));

    [Fact]
    public void Future_birth_date_is_not_adult() =>
        Assert.False(BirthDate.Create(new DateOnly(2026, 10, 13)).IsAtLeast18On(Today));

    [Fact]
    public void Leap_day_birthday_uses_dateonly_addyears_rule() =>
        Assert.True(BirthDate.Create(new DateOnly(2008, 2, 29)).IsAtLeast18On(new DateOnly(2026, 2, 28)));

    [Fact]
    public void Default_date_is_not_a_birth_date() =>
        Assert.Equal(DomainErrorCodes.InvalidBirthDate,
            Assert.Throws<DomainException>(() => BirthDate.Create(default)).Code);
}
