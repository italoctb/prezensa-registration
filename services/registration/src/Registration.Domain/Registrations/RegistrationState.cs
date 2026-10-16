using Registration.Domain.Applicants;

namespace Registration.Domain.Registrations;

public sealed record RegistrationState(
    RegistrationId Id, ApplicantProfile Applicant, RegistrationStatus Status,
    FraudCheckStatus FraudStatus, FailureReason? FailureReason,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    DateTimeOffset? ApprovedAt, ApprovalActor? ApprovedBy, long Version);
