using Registration.Domain.Drafts;

namespace Registration.Application.Abstractions.Persistence;

public interface IRegistrationDraftRepository
{
    Task<RegistrationDraft?> GetByIdAsync(RegistrationDraftId id, CancellationToken cancellationToken);
    Task<RegistrationDraft?> GetActiveByOwnerAsync(OwnerId ownerId, CancellationToken cancellationToken);
    Task<RegistrationDraft> AddOrGetActiveAsync(RegistrationDraft draft, CancellationToken cancellationToken);
    Task UpdateAsync(RegistrationDraft draft, long expectedVersion, CancellationToken cancellationToken);
}
