using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Registration.Application;
using Registration.Domain.Applicants;
using Registration.Domain.Registrations;
using Registration.Infrastructure.Persistence.Oracle;
using Registration.Infrastructure.Tests.Support;

namespace Registration.Infrastructure.Tests.Persistence;

public sealed class OracleRegistrationRepositoryTests : IAsyncLifetime
{
    private const string Password = "TestPass123!";
    private readonly IContainer _container = new ContainerBuilder("gvenzl/oracle-free:23-slim-faststart")
        .WithEnvironment("ORACLE_PASSWORD", Password)
        .WithPortBinding(1521, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("DATABASE IS READY TO USE!"))
        .Build();
    private OracleRegistrationRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var connection = $"User Id=system;Password={Password};Data Source={_container.Hostname}:{_container.GetMappedPublicPort(1521)}/FREEPDB1;";
        var schema = new OracleRegistrationSchemaInitializer(connection);
        await schema.EnsureCreatedAsync();
        await schema.EnsureCreatedAsync();
        _repository = new OracleRegistrationRepository(connection);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Registration_round_trip_preserves_profile_and_state()
    {
        var draft = Sample.Draft();
        var registration = Sample.Registration();
        var persistedId = await _repository.AddForDraftAsync(registration, draft.Id, CancellationToken.None);
        Assert.Equal(registration.Id, persistedId);
        Assert.True(await _repository.ExistsByCpfAsync(registration.Applicant.Cpf, CancellationToken.None));
        Assert.Equal(registration.Id, (await _repository.FindByDraftIdAsync(draft.Id, CancellationToken.None))?.RegistrationId);

        var loaded = await _repository.GetByIdAsync(registration.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(registration.Applicant, loaded.Applicant);
        Assert.Equal(RegistrationStatus.Received, loaded.Status);
        Assert.Empty(loaded.DomainEvents);
        loaded.StartProcessing(Sample.T1);
        await _repository.UpdateAsync(loaded, 1, CancellationToken.None);
        var changed = await _repository.GetByIdAsync(registration.Id, CancellationToken.None);
        Assert.Equal(RegistrationStatus.Processing, changed?.Status);
        Assert.Equal(2L, changed?.Version);
    }

    [Fact]
    public async Task Source_draft_is_idempotent_and_different_draft_cannot_reuse_cpf()
    {
        var firstDraft = Sample.Draft();
        var first = Sample.Registration();
        await _repository.AddForDraftAsync(first, firstDraft.Id, CancellationToken.None);
        var repeated = Sample.Registration();
        Assert.Equal(first.Id, await _repository.AddForDraftAsync(repeated, firstDraft.Id, CancellationToken.None));

        var error = await Assert.ThrowsAsync<RegistrationApplicationException>(() =>
            _repository.AddForDraftAsync(Sample.Registration(), Sample.Draft().Id, CancellationToken.None));
        Assert.Equal(ApplicationErrorCodes.CpfAlreadyRegistered, error.Code);
    }

    [Fact]
    public async Task Same_draft_with_changed_profile_is_a_conflict()
    {
        var draft = Sample.Draft();
        var first = Sample.Registration();
        await _repository.AddForDraftAsync(first, draft.Id, CancellationToken.None);
        var changedProfile = ApplicantProfile.Create(first.Applicant.FullName, first.Applicant.Cpf,
            first.Applicant.BirthDate, EmailAddress.Create("outra@example.com"), first.Applicant.Phone,
            first.Applicant.MotherName, first.Applicant.Nationality, first.Applicant.Address,
            first.Applicant.Profession, first.Applicant.MonthlyIncome);
        var changed = Registration.Domain.Registrations.Registration.Create(
            RegistrationId.From(Guid.NewGuid()), changedProfile, Sample.T0);

        var error = await Assert.ThrowsAsync<RegistrationApplicationException>(() =>
            _repository.AddForDraftAsync(changed, draft.Id, CancellationToken.None));

        Assert.Equal(ApplicationErrorCodes.ConcurrencyConflict, error.Code);
    }

    [Fact]
    public async Task Version_constraint_rejects_a_stale_registration_update()
    {
        var registration = Sample.Registration();
        await _repository.AddForDraftAsync(registration, Sample.Draft().Id, CancellationToken.None);
        var first = await _repository.GetByIdAsync(registration.Id, CancellationToken.None);
        var stale = await _repository.GetByIdAsync(registration.Id, CancellationToken.None);
        Assert.NotNull(first);
        Assert.NotNull(stale);
        first.StartProcessing(Sample.T1);
        await _repository.UpdateAsync(first, 1, CancellationToken.None);
        stale.StartProcessing(Sample.T1);
        var error = await Assert.ThrowsAsync<RegistrationApplicationException>(() =>
            _repository.UpdateAsync(stale, 1, CancellationToken.None));
        Assert.Equal(ApplicationErrorCodes.ConcurrencyConflict, error.Code);
    }
}
