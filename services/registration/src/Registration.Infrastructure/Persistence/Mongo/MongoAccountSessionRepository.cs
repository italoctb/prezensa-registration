using MongoDB.Bson;
using MongoDB.Driver;
using Registration.Application.Abstractions.Persistence;
using Registration.Application.Accounts;
using Registration.Domain.Drafts;

namespace Registration.Infrastructure.Persistence.Mongo;

public sealed class MongoAccountSessionRepository(IMongoDatabase database) : IAccountSessionRepository
{
    private readonly IMongoCollection<BsonDocument> _sessions = database.GetCollection<BsonDocument>("registration_sessions");
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private bool _indexReady;

    public async Task CreateAsync(AccountSession session, CancellationToken cancellationToken)
    {
        await EnsureIndexAsync(cancellationToken);
        await _sessions.InsertOneAsync(new BsonDocument
        {
            ["_id"] = session.TokenHash,
            ["ownerId"] = new BsonBinaryData(session.OwnerId.Value, GuidRepresentation.Standard),
            ["createdAt"] = session.CreatedAt.UtcDateTime,
            ["expiresAt"] = session.ExpiresAt.UtcDateTime
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountSession?> FindActiveAsync(string tokenHash, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", tokenHash) &
            Builders<BsonDocument>.Filter.Gt("expiresAt", now.UtcDateTime);
        var document = await _sessions.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : new AccountSession(tokenHash,
            OwnerId.From(document["ownerId"].AsGuid),
            new DateTimeOffset(document["createdAt"].ToUniversalTime()),
            new DateTimeOffset(document["expiresAt"].ToUniversalTime()));
    }

    public Task RevokeAsync(string tokenHash, CancellationToken cancellationToken) =>
        _sessions.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", tokenHash), cancellationToken);

    private async Task EnsureIndexAsync(CancellationToken cancellationToken)
    {
        if (_indexReady) return;
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_indexReady) return;
            await _sessions.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("expiresAt"),
                new CreateIndexOptions { Name = "ttl_registration_sessions", ExpireAfter = TimeSpan.Zero }),
                cancellationToken: cancellationToken);
            _indexReady = true;
        }
        finally { _indexLock.Release(); }
    }
}
