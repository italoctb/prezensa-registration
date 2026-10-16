using Registration.Domain.Applicants;
using Registration.Domain.Registrations;
using Registration.Domain.Drafts;

namespace Registration.Application.Abstractions.Persistence;

public interface IRegistrationRepository
{
    Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken);
    Task<RegistrationSubmission?> FindByDraftIdAsync(RegistrationDraftId draftId, CancellationToken cancellationToken);
    Task<RegistrationId> AddForDraftAsync(Registration.Domain.Registrations.Registration registration,
        RegistrationDraftId draftId, CancellationToken cancellationToken);
}

public sealed record RegistrationSubmission(RegistrationId RegistrationId, ApplicantProfile Applicant);
