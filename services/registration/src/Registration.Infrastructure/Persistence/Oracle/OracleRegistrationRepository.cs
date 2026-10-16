using Oracle.ManagedDataAccess.Client;
using Registration.Application;
using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Applicants;
using Registration.Domain.Drafts;
using Registration.Domain.Registrations;
using RegistrationAggregate = Registration.Domain.Registrations.Registration;

namespace Registration.Infrastructure.Persistence.Oracle;

public sealed class OracleRegistrationRepository(string connectionString) : IRegistrationRepository
{
    public async Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = new OracleCommand("SELECT 1 FROM REGISTRATIONS WHERE CPF = :cpf FETCH FIRST 1 ROWS ONLY", connection)
        {
            BindByName = true
        };
        Bind(command, "cpf", OracleDbType.Char, cpf.Digits);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<RegistrationSubmission?> FindByDraftIdAsync(RegistrationDraftId draftId, CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = new OracleCommand(
            "SELECT ID, APPLICANT_JSON FROM REGISTRATIONS WHERE SOURCE_DRAFT_ID = :draft_id", connection)
        {
            BindByName = true
        };
        Bind(command, "draft_id", OracleDbType.Varchar2, draftId.Value.ToString("D"));
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new RegistrationSubmission(RegistrationId.From(Guid.Parse(reader.GetString(0))),
                ApplicantJsonMapper.ToDomain(reader.GetString(1)))
            : null;
    }

    public async Task<RegistrationId> AddForDraftAsync(
        RegistrationAggregate registration, RegistrationDraftId draftId, CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = new OracleCommand("""
            INSERT INTO REGISTRATIONS
                (ID, SOURCE_DRAFT_ID, CPF, APPLICANT_JSON, STATUS, FRAUD_STATUS, FAILURE_REASON,
                 CREATED_AT_TICKS, UPDATED_AT_TICKS, APPROVED_AT_TICKS, APPROVED_BY, VERSION)
            VALUES
                (:id, :draft_id, :cpf, :applicant, :status, :fraud_status, :failure_reason,
                 :created_at, :updated_at, :approved_at, :approved_by, :version)
            """, connection)
        {
            BindByName = true
        };
        Bind(command, "id", OracleDbType.Varchar2, registration.Id.Value.ToString("D"));
        Bind(command, "draft_id", OracleDbType.Varchar2, draftId.Value.ToString("D"));
        Bind(command, "cpf", OracleDbType.Char, registration.Applicant.Cpf.Digits);
        Bind(command, "applicant", OracleDbType.Clob, ApplicantJsonMapper.ToJson(registration.Applicant));
        BindMutableState(command, registration, includeCreatedAt: true);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return registration.Id;
        }
        catch (OracleException ex) when (ex.Number == 1)
        {
            var existing = await FindByDraftIdAsync(draftId, cancellationToken);
            if (existing is not null)
            {
                if (existing.Applicant != registration.Applicant)
                    throw new RegistrationApplicationException(ApplicationErrorCodes.ConcurrencyConflict,
                        "Draft differs from the registration already created for it.");
                return existing.RegistrationId;
            }
            if (await ExistsByCpfAsync(registration.Applicant.Cpf, cancellationToken))
                throw new RegistrationApplicationException(ApplicationErrorCodes.CpfAlreadyRegistered,
                    "CPF is already registered.");
            throw;
        }
    }

    public async Task<RegistrationAggregate?> GetByIdAsync(RegistrationId id, CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = new OracleCommand("""
            SELECT ID, APPLICANT_JSON, STATUS, FRAUD_STATUS, FAILURE_REASON, CREATED_AT_TICKS,
                   UPDATED_AT_TICKS, APPROVED_AT_TICKS, APPROVED_BY, VERSION
            FROM REGISTRATIONS WHERE ID = :id
            """, connection)
        {
            BindByName = true
        };
        Bind(command, "id", OracleDbType.Varchar2, id.Value.ToString("D"));
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return RegistrationAggregate.Restore(new RegistrationState(
            RegistrationId.From(Guid.Parse(reader.GetString(0))),
            ApplicantJsonMapper.ToDomain(reader.GetString(1)),
            Enum.Parse<RegistrationStatus>(reader.GetString(2)),
            Enum.Parse<FraudCheckStatus>(reader.GetString(3)),
            reader.IsDBNull(4) ? null : FailureReason.Create(reader.GetString(4)),
            Utc(reader.GetInt64(5)), Utc(reader.GetInt64(6)),
            reader.IsDBNull(7) ? null : Utc(reader.GetInt64(7)),
            reader.IsDBNull(8) ? null : ApprovalActor.Create(reader.GetString(8)),
            reader.GetInt64(9)));
    }

    public async Task UpdateAsync(RegistrationAggregate registration, long expectedVersion, CancellationToken cancellationToken)
    {
        if (registration.Version <= expectedVersion)
            throw new ArgumentOutOfRangeException(nameof(expectedVersion), "Updated registration version must advance.");
        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = new OracleCommand("""
            UPDATE REGISTRATIONS
               SET STATUS = :status, FRAUD_STATUS = :fraud_status, FAILURE_REASON = :failure_reason,
                   UPDATED_AT_TICKS = :updated_at, APPROVED_AT_TICKS = :approved_at,
                   APPROVED_BY = :approved_by, VERSION = :version
             WHERE ID = :id AND VERSION = :expected_version
            """, connection)
        {
            BindByName = true
        };
        BindMutableState(command, registration, includeCreatedAt: false);
        Bind(command, "id", OracleDbType.Varchar2, registration.Id.Value.ToString("D"));
        Bind(command, "expected_version", OracleDbType.Int64, expectedVersion);
        var changed = await command.ExecuteNonQueryAsync(cancellationToken);
        if (changed != 1)
            throw new RegistrationApplicationException(ApplicationErrorCodes.ConcurrencyConflict,
                "Registration changed since it was loaded.");
    }

    private static void BindMutableState(OracleCommand command, RegistrationAggregate registration, bool includeCreatedAt)
    {
        Bind(command, "status", OracleDbType.Varchar2, registration.Status.ToString());
        Bind(command, "fraud_status", OracleDbType.Varchar2, registration.FraudStatus.ToString());
        Bind(command, "failure_reason", OracleDbType.Varchar2, registration.FailureReason?.Value);
        if (includeCreatedAt)
            Bind(command, "created_at", OracleDbType.Int64, registration.CreatedAt.UtcTicks);
        Bind(command, "updated_at", OracleDbType.Int64, registration.UpdatedAt.UtcTicks);
        Bind(command, "approved_at", OracleDbType.Int64, registration.ApprovedAt?.UtcTicks);
        Bind(command, "approved_by", OracleDbType.Varchar2, registration.ApprovedBy?.Value);
        Bind(command, "version", OracleDbType.Int64, registration.Version);
    }

    private static void Bind(OracleCommand command, string name, OracleDbType type, object? value) =>
        command.Parameters.Add(new OracleParameter(name, type) { Value = value ?? DBNull.Value });

    private static DateTimeOffset Utc(long ticks) => new(ticks, TimeSpan.Zero);
}
