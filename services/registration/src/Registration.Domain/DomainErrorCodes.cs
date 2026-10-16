namespace Registration.Domain;

public static class DomainErrorCodes
{
    public const string InvalidId = "registration.invalid_id";
    public const string InvalidApplicantProfile = "registration.invalid_applicant_profile";
    public const string InvalidName = "registration.invalid_name";
    public const string InvalidCpf = "registration.invalid_cpf";
    public const string InvalidBirthDate = "registration.invalid_birth_date";
    public const string UnderageApplicant = "registration.underage_applicant";
    public const string InvalidEmail = "registration.invalid_email";
    public const string InvalidPhone = "registration.invalid_phone";
    public const string InvalidMotherName = "registration.invalid_mother_name";
    public const string InvalidNationality = "registration.invalid_nationality";
    public const string InvalidAddress = "registration.invalid_address";
    public const string InvalidProfession = "registration.invalid_profession";
    public const string InvalidMonthlyIncome = "registration.invalid_monthly_income";
    public const string InvalidApprovalActor = "registration.invalid_approval_actor";
    public const string InvalidFailureReason = "registration.invalid_failure_reason";
    public const string InvalidTimestamp = "registration.invalid_timestamp";
    public const string InvalidTransition = "registration.invalid_transition";
    public const string IncompleteDraft = "registration.incomplete_draft";
}
