using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;
using Registration.Domain.Drafts;
using Registration.Domain.Registrations;

namespace Registration.Application.Drafts;

public sealed record DraftSnapshot(
    RegistrationDraftId DraftId, OwnerId OwnerId, RegistrationDraftStatus Status,
    RegistrationDraftStep CurrentStep, FullName? FullName, Cpf? Cpf, BirthDate? BirthDate,
    EmailAddress? Email, PhoneNumber? Phone, MotherName? MotherName, Nationality? Nationality,
    Address? Address, Profession? Profession, MonthlyIncome? MonthlyIncome,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? SubmittedAt,
    RegistrationId? RegistrationId, long Version)
{
    public static DraftSnapshot From(RegistrationDraft draft) =>
        new(draft.Id, draft.OwnerId, draft.Status, draft.CurrentStep, draft.FullName, draft.Cpf,
            draft.BirthDate, draft.Email, draft.Phone, draft.MotherName, draft.Nationality,
            draft.Address, draft.Profession, draft.MonthlyIncome, draft.CreatedAt,
            draft.UpdatedAt, draft.SubmittedAt, draft.RegistrationId, draft.Version);
}
