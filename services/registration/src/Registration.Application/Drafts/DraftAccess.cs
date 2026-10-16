using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Drafts;

namespace Registration.Application.Drafts;

internal static class DraftAccess
{
    public static async Task<RegistrationDraft> LoadOwnedAsync(
        IRegistrationDraftRepository repository, RegistrationDraftId id, OwnerId ownerId,
        CancellationToken cancellationToken)
    {
        var draft = await repository.GetByIdAsync(id, cancellationToken);
        if (draft is null)
            throw new RegistrationApplicationException(ApplicationErrorCodes.DraftNotFound, "Draft was not found.");
        if (draft.OwnerId != ownerId)
            throw new RegistrationApplicationException(ApplicationErrorCodes.DraftAccessDenied, "Draft belongs to another owner.");
        return draft;
    }
}
