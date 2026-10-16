using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Drafts;

namespace Registration.Application.Drafts.Start;

public sealed record StartDraftCommand(OwnerId OwnerId);

public sealed class StartDraftHandler(IRegistrationDraftRepository drafts, TimeProvider timeProvider)
{
    public async Task<DraftSnapshot> HandleAsync(StartDraftCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await drafts.GetActiveByOwnerAsync(command.OwnerId, cancellationToken);
        if (existing is not null)
            return DraftSnapshot.From(existing);

        var draft = RegistrationDraft.Start(RegistrationDraftId.From(Guid.NewGuid()), command.OwnerId,
            timeProvider.GetUtcNow());
        var active = await drafts.AddOrGetActiveAsync(draft, cancellationToken);
        return DraftSnapshot.From(active);
    }
}
