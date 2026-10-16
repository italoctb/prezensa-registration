using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Registration.Application.Abstractions.Integrations;
using Registration.Application.Abstractions.Persistence;
using Registration.Infrastructure.Integrations;
using Registration.Infrastructure.Persistence.Mongo;
using Registration.Infrastructure.Persistence.Oracle;

namespace Registration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var mongoConnection = Required(configuration, "Registration:Mongo:ConnectionString");
        var mongoDatabase = Required(configuration, "Registration:Mongo:Database");
        var oracleConnection = Required(configuration, "Registration:Oracle:ConnectionString");
        var smtpHost = Required(configuration, "Registration:Authentication:Smtp:Host");
        var smtpFrom = Required(configuration, "Registration:Authentication:Smtp:From");
        if (!int.TryParse(Required(configuration, "Registration:Authentication:Smtp:Port"), out var smtpPort) ||
            smtpPort is < 1 or > 65535)
            throw new InvalidOperationException("Registration:Authentication:Smtp:Port must be a valid port.");
        if (Required(configuration, "Registration:Authentication:Smtp:Security") != "StartTls")
            throw new InvalidOperationException("SMTP security must be StartTls.");
        var username = configuration["Registration:Authentication:Smtp:Username"];
        var password = configuration["Registration:Authentication:Smtp:Password"];
        if (string.IsNullOrWhiteSpace(username) != string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SMTP username and password must be configured together.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabase));
        services.AddSingleton<IRegistrationDraftRepository, MongoRegistrationDraftRepository>();
        services.AddSingleton<IAccountRepository, MongoAccountRepository>();
        services.AddSingleton<IEmailCodeRepository, MongoEmailCodeRepository>();
        services.AddSingleton<IAccountSessionRepository, MongoAccountSessionRepository>();
        services.AddSingleton(new SmtpAuthenticationSettings(smtpHost, smtpPort, smtpFrom,
            username, password));
        services.AddSingleton<IAuthenticationEmailSender, SmtpAuthenticationEmailSender>();
        services.AddSingleton<IRegistrationRepository>(_ => new OracleRegistrationRepository(oracleConnection));
        services.AddSingleton(_ => new OracleRegistrationSchemaInitializer(oracleConnection));
        services.AddHttpClient<IPostalCodeLookup, ViaCepPostalCodeLookup>(client =>
        {
            client.BaseAddress = new Uri("https://viacep.com.br/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        return services;
    }

    private static string Required(IConfiguration configuration, string key) =>
        string.IsNullOrWhiteSpace(configuration[key])
            ? throw new InvalidOperationException($"Configuration '{key}' is required.")
            : configuration[key]!;
}
