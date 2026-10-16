using Registration.Domain.Applicants;
using Registration.Domain.Applicants.Addresses;
using Registration.Domain.Registrations;

namespace Registration.Domain.Drafts;

public sealed record RegistrationDraftState(
    RegistrationDraftId Id, OwnerId OwnerId, RegistrationDraftStatus Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? SubmittedAt,
    RegistrationId? RegistrationId, long Version,
    FullName? FullName, Cpf? Cpf, BirthDate? BirthDate, EmailAddress? Email,
    PhoneNumber? Phone, MotherName? MotherName, Nationality? Nationality,
    Address? Address, Profession? Profession, MonthlyIncome? MonthlyIncome);
