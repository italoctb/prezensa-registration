namespace Registration.Application.Accounts.Login;

public sealed record RequestLoginCodeCommand(string? Email);
public sealed record ConfirmLoginCommand(string? Email, string? Code);

public sealed class RequestLoginCodeHandler(EmailCodeWorkflow workflow)
{
    public Task HandleAsync(RequestLoginCodeCommand command, CancellationToken cancellationToken = default) =>
        workflow.RequestAsync(command.Email, EmailCodePurpose.Login, cancellationToken);
}

public sealed class ConfirmLoginHandler(EmailCodeWorkflow workflow)
{
    public Task<SessionIssued> HandleAsync(ConfirmLoginCommand command,
        CancellationToken cancellationToken = default) =>
        workflow.ConfirmAsync(command.Email, command.Code, EmailCodePurpose.Login,
            cancellationToken);
}
