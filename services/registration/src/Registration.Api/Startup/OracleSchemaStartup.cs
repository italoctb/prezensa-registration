using Registration.Infrastructure.Persistence.Oracle;

namespace Registration.Api.Startup;

public sealed class OracleSchemaStartup(OracleRegistrationSchemaInitializer schema) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => schema.EnsureCreatedAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
