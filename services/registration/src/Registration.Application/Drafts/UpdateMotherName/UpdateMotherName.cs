using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Applicants;
using Registration.Domain.Drafts;

namespace Registration.Application.Drafts.UpdateMotherName;

public sealed record UpdateMotherNameCommand(RegistrationDraftId DraftId, OwnerId OwnerId, string? MotherName);

public sealed class UpdateMotherNameHandler(IRegistrationDraftRepository drafts, TimeProvider timeProvider)
{
    public async Task<DraftSnapshot> HandleAsync(UpdateMotherNameCommand command, CancellationToken cancellationToken = default)
    {
        var draft = await DraftAccess.LoadOwnedAsync(drafts, command.DraftId, command.OwnerId, cancellationToken);
        var expectedVersion = draft.Version;
        var value = MotherName.Create(command.MotherName);
        draft.SetMotherName(value, timeProvider.GetUtcNow());
        await drafts.UpdateAsync(draft, expectedVersion, cancellationToken);
        return DraftSnapshot.From(draft);
    }
}
