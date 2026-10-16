using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Applicants;
using Registration.Domain.Drafts;

namespace Registration.Application.Drafts.UpdateEmail;

public sealed record UpdateEmailCommand(RegistrationDraftId DraftId, OwnerId OwnerId, string? Email);

public sealed class UpdateEmailHandler(IRegistrationDraftRepository drafts, TimeProvider timeProvider)
{
    public async Task<DraftSnapshot> HandleAsync(UpdateEmailCommand command, CancellationToken cancellationToken = default)
    {
        var draft = await DraftAccess.LoadOwnedAsync(drafts, command.DraftId, command.OwnerId, cancellationToken);
        var expectedVersion = draft.Version;
        var value = EmailAddress.Create(command.Email);
        draft.SetEmail(value, timeProvider.GetUtcNow());
        await drafts.UpdateAsync(draft, expectedVersion, cancellationToken);
        return DraftSnapshot.From(draft);
    }
}
