using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Drafts;

namespace Registration.Application.Drafts.GetCurrent;

public sealed record GetCurrentDraftQuery(OwnerId OwnerId);

public sealed class GetCurrentDraftHandler(IRegistrationDraftRepository drafts)
{
    public async Task<DraftSnapshot?> HandleAsync(GetCurrentDraftQuery query, CancellationToken cancellationToken = default)
    {
        var draft = await drafts.GetActiveByOwnerAsync(query.OwnerId, cancellationToken);
        return draft is null ? null : DraftSnapshot.From(draft);
    }
}
