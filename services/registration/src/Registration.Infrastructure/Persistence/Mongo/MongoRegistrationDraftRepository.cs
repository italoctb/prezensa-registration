using MongoDB.Bson;
using MongoDB.Driver;
using Registration.Application;
using Registration.Application.Abstractions.Persistence;
using Registration.Domain.Drafts;

namespace Registration.Infrastructure.Persistence.Mongo;

public sealed class MongoRegistrationDraftRepository(IMongoDatabase database) : IRegistrationDraftRepository
{
    private readonly IMongoCollection<BsonDocument> _collection = database.GetCollection<BsonDocument>("registration_drafts");
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private bool _indexReady;

    public async Task<RegistrationDraft?> GetByIdAsync(RegistrationDraftId id, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id.Value.ToString("D"));
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : MongoDraftDocumentMapper.ToDomain(document);
    }

    public async Task<RegistrationDraft?> GetActiveByOwnerAsync(OwnerId ownerId, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("ownerId", ownerId.Value.ToString("D")) &
            Builders<BsonDocument>.Filter.Eq("status", RegistrationDraftStatus.InProgress.ToString());
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : MongoDraftDocumentMapper.ToDomain(document);
    }

    public async Task<RegistrationDraft> AddOrGetActiveAsync(RegistrationDraft draft, CancellationToken cancellationToken)
    {
        await EnsureIndexAsync(cancellationToken);
        try
        {
            await _collection.InsertOneAsync(MongoDraftDocumentMapper.ToDocument(draft), cancellationToken: cancellationToken);
            return draft;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return await GetActiveByOwnerAsync(draft.OwnerId, cancellationToken)
                ?? throw new RegistrationApplicationException(ApplicationErrorCodes.ConcurrencyConflict,
                    "Draft creation conflicted with another change.");
        }
    }

    public async Task UpdateAsync(RegistrationDraft draft, long expectedVersion, CancellationToken cancellationToken)
    {
        if (draft.Version <= expectedVersion)
            throw new ArgumentOutOfRangeException(nameof(expectedVersion), "Updated draft version must advance.");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", draft.Id.Value.ToString("D")) &
            Builders<BsonDocument>.Filter.Eq("version", expectedVersion);
        var result = await _collection.ReplaceOneAsync(filter, MongoDraftDocumentMapper.ToDocument(draft),
            cancellationToken: cancellationToken);
        if (result.MatchedCount != 1)
            throw new RegistrationApplicationException(ApplicationErrorCodes.ConcurrencyConflict,
                "Draft changed since it was loaded.");
    }

    private async Task EnsureIndexAsync(CancellationToken cancellationToken)
    {
        if (_indexReady) return;
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_indexReady) return;
            var options = new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_registration_drafts_active_owner",
                Unique = true,
                PartialFilterExpression = Builders<BsonDocument>.Filter.Eq("status", RegistrationDraftStatus.InProgress.ToString())
            };
            await _collection.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("ownerId"), options),
                cancellationToken: cancellationToken);
            _indexReady = true;
        }
        finally
        {
            _indexLock.Release();
        }
    }
}
