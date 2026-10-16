using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Registration.Api.Errors;
using Registration.Api.Security;
using Registration.Api.Startup;
using Registration.Application.Accounts;
using Registration.Application.Accounts.Login;
using Registration.Application.Accounts.Register;
using Registration.Application.Accounts.Sessions;
using Registration.Application.Drafts.GetCurrent;
using Registration.Application.Drafts.LookupPostalCode;
using Registration.Application.Drafts.Start;
using Registration.Application.Drafts.UpdateAddress;
using Registration.Application.Drafts.UpdateBirthDate;
using Registration.Application.Drafts.UpdateDocument;
using Registration.Application.Drafts.UpdateEmail;
using Registration.Application.Drafts.UpdateFullName;
using Registration.Application.Drafts.UpdateMonthlyIncome;
using Registration.Application.Drafts.UpdateMotherName;
using Registration.Application.Drafts.UpdateNationality;
using Registration.Application.Drafts.UpdatePhone;
using Registration.Application.Drafts.UpdateProfession;
using Registration.Application.Registrations.SubmitDraft;

namespace Registration.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddRegistrationApi(this IServiceCollection services, IConfiguration configuration)
    {
        var key = Required(configuration, "Registration:Authentication:CodeHashKey");
        byte[] keyBytes;
        try { keyBytes = Convert.FromBase64String(key); }
        catch (FormatException error)
        {
            throw new InvalidOperationException("CodeHashKey must be base64 encoded.", error);
        }
        services.AddSingleton(new AuthenticationPolicy(keyBytes));
        services.AddScoped<EmailCodeWorkflow>();
        services.AddScoped<RequestAccountCodeHandler>();
        services.AddScoped<ConfirmAccountHandler>();
        services.AddScoped<RequestLoginCodeHandler>();
        services.AddScoped<ConfirmLoginHandler>();
        services.AddScoped<SessionAccess>();
        services.AddAuthentication("Bearer")
            .AddScheme<AuthenticationSchemeOptions, OpaqueBearerHandler>("Bearer", _ => { });
        services.AddAuthorization();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("account-access", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });
        services.AddProblemDetails();
        services.AddExceptionHandler<RegistrationExceptionHandler>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<OracleSchemaStartup>();

        services.AddScoped<StartDraftHandler>();
        services.AddScoped<GetCurrentDraftHandler>();
        services.AddScoped<UpdateFullNameHandler>();
        services.AddScoped<UpdateDocumentHandler>();
        services.AddScoped<UpdateBirthDateHandler>();
        services.AddScoped<UpdateEmailHandler>();
        services.AddScoped<UpdatePhoneHandler>();
        services.AddScoped<UpdateMotherNameHandler>();
        services.AddScoped<UpdateNationalityHandler>();
        services.AddScoped<UpdateAddressHandler>();
        services.AddScoped<UpdateProfessionHandler>();
        services.AddScoped<UpdateMonthlyIncomeHandler>();
        services.AddScoped<LookupPostalCodeHandler>();
        services.AddScoped<SubmitRegistrationDraftHandler>();
        return services;
    }

    private static string Required(IConfiguration configuration, string key) =>
        string.IsNullOrWhiteSpace(configuration[key])
            ? throw new InvalidOperationException($"Configuration '{key}' is required.")
            : configuration[key]!;
}
