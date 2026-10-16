using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Driver;
using Registration.Application;
using Registration.Domain.Applicants;
using Registration.Domain.Drafts;
using Registration.Domain.Registrations;
using Registration.Infrastructure.Persistence.Mongo;
using Registration.Infrastructure.Tests.Support;

namespace Registration.Infrastructure.Tests.Persistence;

public sealed class MongoRegistrationDraftRepositoryTests : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder("mongo:8.0")
        .WithPortBinding(27017, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(27017))
        .Build();
    private MongoRegistrationDraftRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var connection = $"mongodb://{_container.Hostname}:{_container.GetMappedPublicPort(27017)}";
        var database = new MongoClient(connection).GetDatabase($"registration_tests_{Guid.NewGuid():N}");
        _repository = new MongoRegistrationDraftRepository(database);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Draft_round_trip_preserves_partial_values_and_version()
    {
        var draft = Sample.Draft();
        var persisted = await _repository.AddOrGetActiveAsync(draft, CancellationToken.None);
        Assert.Equal(draft.Id, persisted.Id);
        var loaded = await _repository.GetByIdAsync(draft.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Empty(loaded.DomainEvents);
        var expectedVersion = loaded.Version;
        loaded.SetFullName(FullName.Create("Ana Maria"), Sample.T1);
        await _repository.UpdateAsync(loaded, expectedVersion, CancellationToken.None);
        var again = await _repository.GetByIdAsync(draft.Id, CancellationToken.None);
        Assert.Equal(FullName.Create("Ana Maria"), again?.FullName);
        Assert.Equal(2L, again?.Version);
        Assert.Equal(Sample.T1, again?.UpdatedAt);
    }

    [Fact]
    public async Task Unique_active_owner_handles_racing_starts()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var first = Sample.Draft(owner);
        var second = Sample.Draft(owner);
        var results = await Task.WhenAll(
            _repository.AddOrGetActiveAsync(first, CancellationToken.None),
            _repository.AddOrGetActiveAsync(second, CancellationToken.None));
        Assert.Equal(results[0].Id, results[1].Id);
        Assert.Equal(results[0].Id, (await _repository.GetActiveByOwnerAsync(owner, CancellationToken.None))?.Id);
    }

    [Fact]
    public async Task Version_filter_rejects_stale_writer()
    {
        var draft = Sample.Draft();
        await _repository.AddOrGetActiveAsync(draft, CancellationToken.None);
        var first = await _repository.GetByIdAsync(draft.Id, CancellationToken.None);
        var stale = await _repository.GetByIdAsync(draft.Id, CancellationToken.None);
        Assert.NotNull(first);
        Assert.NotNull(stale);
        first.SetFullName(FullName.Create("Ana Maria"), Sample.T1);
        await _repository.UpdateAsync(first, 1, CancellationToken.None);
        stale.SetFullName(FullName.Create("Ana Lima"), Sample.T1);
        var error = await Assert.ThrowsAsync<RegistrationApplicationException>(() =>
            _repository.UpdateAsync(stale, 1, CancellationToken.None));
        Assert.Equal(ApplicationErrorCodes.ConcurrencyConflict, error.Code);
    }

    [Fact]
    public async Task Submitted_draft_releases_unique_active_owner_index()
    {
        var draft = Sample.Draft();
        await _repository.AddOrGetActiveAsync(draft, CancellationToken.None);
        Sample.Complete(draft);
        draft.MarkSubmitted(RegistrationId.From(Guid.NewGuid()), Sample.T2);
        await _repository.UpdateAsync(draft, 1, CancellationToken.None);
        var restored = await _repository.GetByIdAsync(draft.Id, CancellationToken.None);
        Assert.Equal(RegistrationDraftStatus.Submitted, restored?.Status);
        Assert.Equal(draft.RegistrationId, restored?.RegistrationId);
        Assert.Null(await _repository.GetActiveByOwnerAsync(draft.OwnerId, CancellationToken.None));
        var next = Sample.Draft(draft.OwnerId);
        await _repository.AddOrGetActiveAsync(next, CancellationToken.None);
        Assert.Equal(next.Id, (await _repository.GetActiveByOwnerAsync(draft.OwnerId, CancellationToken.None))?.Id);
    }
}
