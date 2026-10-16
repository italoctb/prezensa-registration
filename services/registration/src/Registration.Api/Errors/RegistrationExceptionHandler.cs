using Microsoft.AspNetCore.Diagnostics;
using Registration.Api.Security;
using Registration.Application;
using Registration.Application.Accounts;
using Registration.Domain;

namespace Registration.Api.Errors;

public sealed class RegistrationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var mapped = exception switch
        {
            DomainException domain => (StatusCodes.Status400BadRequest, domain.Code, domain.Message),
            RegistrationApplicationException application =>
                (Status(application.Code), application.Code, application.Message),
            OwnerClaimException => (StatusCodes.Status403Forbidden, "registration.invalid_owner_claim",
                "Authenticated subject must be a non-empty GUID."),
            _ => ((int Status, string Code, string Detail)?)null
        };
        if (mapped is null) return false;

        await Results.Problem(statusCode: mapped.Value.Status, title: "Request failed",
            detail: mapped.Value.Detail,
            extensions: new Dictionary<string, object?> { ["code"] = mapped.Value.Code })
            .ExecuteAsync(context);
        return true;
    }

    private static int Status(string code) => code switch
    {
        AccountErrorCodes.InvalidCorporateEmail => StatusCodes.Status400BadRequest,
        AccountErrorCodes.InvalidCode => StatusCodes.Status401Unauthorized,
        AccountErrorCodes.AccountAlreadyExists => StatusCodes.Status409Conflict,
        ApplicationErrorCodes.DraftNotFound or ApplicationErrorCodes.PostalCodeNotFound =>
            StatusCodes.Status404NotFound,
        ApplicationErrorCodes.DraftAccessDenied => StatusCodes.Status403Forbidden,
        ApplicationErrorCodes.CpfAlreadyRegistered or ApplicationErrorCodes.ConcurrencyConflict =>
            StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
