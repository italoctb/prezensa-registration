namespace Registration.Application.Accounts.Register;

public sealed record RequestAccountCodeCommand(string? Email);
public sealed record ConfirmAccountCommand(string? Email, string? Code);

public sealed class RequestAccountCodeHandler(EmailCodeWorkflow workflow)
{
    public Task HandleAsync(RequestAccountCodeCommand command, CancellationToken cancellationToken = default) =>
        workflow.RequestAsync(command.Email, EmailCodePurpose.CreateAccount, cancellationToken);
}

public sealed class ConfirmAccountHandler(EmailCodeWorkflow workflow)
{
    public Task<SessionIssued> HandleAsync(ConfirmAccountCommand command,
        CancellationToken cancellationToken = default) =>
        workflow.ConfirmAsync(command.Email, command.Code, EmailCodePurpose.CreateAccount,
            cancellationToken);
}
