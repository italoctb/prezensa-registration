using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Applicants;
using Registration.Domain.Drafts;

namespace Registration.Application.Drafts.UpdateMonthlyIncome;

public sealed record UpdateMonthlyIncomeCommand(RegistrationDraftId DraftId, OwnerId OwnerId, decimal MonthlyIncome);

public sealed class UpdateMonthlyIncomeHandler(IRegistrationDraftRepository drafts, TimeProvider timeProvider)
{
    public async Task<DraftSnapshot> HandleAsync(UpdateMonthlyIncomeCommand command, CancellationToken cancellationToken = default)
    {
        var draft = await DraftAccess.LoadOwnedAsync(drafts, command.DraftId, command.OwnerId, cancellationToken);
        var expectedVersion = draft.Version;
        var value = MonthlyIncome.Create(command.MonthlyIncome);
        draft.SetMonthlyIncome(value, timeProvider.GetUtcNow());
        await drafts.UpdateAsync(draft, expectedVersion, cancellationToken);
        return DraftSnapshot.From(draft);
    }
}
