using System.Reflection;
using Oracle.ManagedDataAccess.Client;

namespace Registration.Infrastructure.Persistence.Oracle;

public sealed class OracleRegistrationSchemaInitializer(string connectionString)
{
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var check = new OracleCommand(
            "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = 'REGISTRATIONS'", connection);
        var exists = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken));
        if (exists != 0) return;

        var assembly = typeof(OracleRegistrationSchemaInitializer).Assembly;
        const string resource = "Registration.Infrastructure.Persistence.Oracle.Sql.001_registrations.sql";
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Missing embedded schema resource: {resource}.");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(cancellationToken);
        using var create = new OracleCommand(sql, connection);
        try
        {
            await create.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OracleException ex) when (ex.Number == 955)
        {
            // Another initializer created the same table concurrently.
        }
    }
}
