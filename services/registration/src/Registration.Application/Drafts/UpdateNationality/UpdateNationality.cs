using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Applicants;
using Registration.Domain.Drafts;

namespace Registration.Application.Drafts.UpdateNationality;

public sealed record UpdateNationalityCommand(RegistrationDraftId DraftId, OwnerId OwnerId, string? Nationality);

public sealed class UpdateNationalityHandler(IRegistrationDraftRepository drafts, TimeProvider timeProvider)
{
    public async Task<DraftSnapshot> HandleAsync(UpdateNationalityCommand command, CancellationToken cancellationToken = default)
    {
        var draft = await DraftAccess.LoadOwnedAsync(drafts, command.DraftId, command.OwnerId, cancellationToken);
        var expectedVersion = draft.Version;
        var value = Nationality.Create(command.Nationality);
        draft.SetNationality(value, timeProvider.GetUtcNow());
        await drafts.UpdateAsync(draft, expectedVersion, cancellationToken);
        return DraftSnapshot.From(draft);
    }
}
