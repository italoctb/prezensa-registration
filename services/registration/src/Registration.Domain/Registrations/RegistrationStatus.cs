namespace Registration.Domain.Registrations;

public enum RegistrationStatus
{
    Received,
    Processing,
    Completed,
    Rejected,
    Failed,
    Cancelled
}
