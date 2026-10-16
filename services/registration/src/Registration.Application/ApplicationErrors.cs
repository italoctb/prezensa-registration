namespace Registration.Application;

public static class ApplicationErrorCodes
{
    public const string DraftNotFound = "registration.draft_not_found";
    public const string DraftAccessDenied = "registration.draft_access_denied";
    public const string CpfAlreadyRegistered = "registration.cpf_already_registered";
    public const string PostalCodeNotFound = "registration.postal_code_not_found";
    public const string ConcurrencyConflict = "registration.concurrency_conflict";
}

public sealed class RegistrationApplicationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
