using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Applicants;
using Registration.Domain.Drafts;

namespace Registration.Application.Drafts.UpdateProfession;

public sealed record UpdateProfessionCommand(RegistrationDraftId DraftId, OwnerId OwnerId, string? Profession);

public sealed class UpdateProfessionHandler(IRegistrationDraftRepository drafts, TimeProvider timeProvider)
{
    public async Task<DraftSnapshot> HandleAsync(UpdateProfessionCommand command, CancellationToken cancellationToken = default)
    {
        var draft = await DraftAccess.LoadOwnedAsync(drafts, command.DraftId, command.OwnerId, cancellationToken);
        var expectedVersion = draft.Version;
        var value = Profession.Create(command.Profession);
        draft.SetProfession(value, timeProvider.GetUtcNow());
        await drafts.UpdateAsync(draft, expectedVersion, cancellationToken);
        return DraftSnapshot.From(draft);
    }
}
