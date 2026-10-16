using MongoDB.Bson;
using MongoDB.Driver;
using Registration.Application.Abstractions.Persistence;
using Registration.Application.Accounts;

namespace Registration.Infrastructure.Persistence.Mongo;

public sealed class MongoEmailCodeRepository(IMongoDatabase database) : IEmailCodeRepository
{
    private readonly IMongoCollection<BsonDocument> _codes = database.GetCollection<BsonDocument>("registration_email_codes");
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private bool _indexReady;

    public async Task<bool> TryIssueAsync(EmailCodeChallenge challenge, TimeSpan resendInterval,
        int maxFailedAttempts, CancellationToken cancellationToken)
    {
        await EnsureIndexAsync(cancellationToken);
        var id = Key(challenge.Email, challenge.Purpose);
        for (var retry = 0; retry < 8; retry++)
        {
            var current = await _codes.Find(Builders<BsonDocument>.Filter.Eq("_id", id))
                .FirstOrDefaultAsync(cancellationToken);
            if (current is not null)
            {
                var expires = new DateTimeOffset(current["expiresAt"].ToUniversalTime());
                var issued = new DateTimeOffset(current["issuedAt"].ToUniversalTime());
                if (expires > challenge.IssuedAt &&
                    (issued.Add(resendInterval) > challenge.IssuedAt ||
                     current["failedAttempts"].AsInt32 >= maxFailedAttempts))
                    return false;
            }

            var version = current is null ? 1L : current["version"].AsInt64 + 1;
            var failures = current is not null &&
                new DateTimeOffset(current["expiresAt"].ToUniversalTime()) > challenge.IssuedAt
                ? current["failedAttempts"].AsInt32 : 0;
            var replacement = new BsonDocument
            {
                ["_id"] = id,
                ["hash"] = challenge.CodeHash,
                ["issuedAt"] = challenge.IssuedAt.UtcDateTime,
                ["expiresAt"] = challenge.ExpiresAt.UtcDateTime,
                ["failedAttempts"] = failures,
                ["consumed"] = false,
                ["version"] = version
            };

            if (current is null)
            {
                try
                {
                    await _codes.InsertOneAsync(replacement, cancellationToken: cancellationToken);
                    return true;
                }
                catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    continue;
                }
            }
            else
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id) &
                    Builders<BsonDocument>.Filter.Eq("version", current["version"].AsInt64);
                var result = await _codes.ReplaceOneAsync(filter, replacement,
                    cancellationToken: cancellationToken);
                if (result.ModifiedCount == 1) return true;
            }
        }
        return false;
    }

    public async Task<bool> TryConsumeAsync(CorporateEmail email, EmailCodePurpose purpose,
        string codeHash, DateTimeOffset now, int maxFailedAttempts, CancellationToken cancellationToken)
    {
        var active = Builders<BsonDocument>.Filter.Eq("_id", Key(email, purpose)) &
            Builders<BsonDocument>.Filter.Gt("expiresAt", now.UtcDateTime) &
            Builders<BsonDocument>.Filter.Eq("consumed", false) &
            Builders<BsonDocument>.Filter.Lt("failedAttempts", maxFailedAttempts);
        var matched = await _codes.FindOneAndUpdateAsync(
            active & Builders<BsonDocument>.Filter.Eq("hash", codeHash),
            Builders<BsonDocument>.Update.Combine(
                Builders<BsonDocument>.Update.Set("consumed", true),
                Builders<BsonDocument>.Update.Inc("version", 1)),
            cancellationToken: cancellationToken);
        if (matched is not null) return true;

        await _codes.UpdateOneAsync(active,
            Builders<BsonDocument>.Update.Combine(
                Builders<BsonDocument>.Update.Inc("failedAttempts", 1),
                Builders<BsonDocument>.Update.Inc("version", 1)),
            cancellationToken: cancellationToken);
        return false;
    }

    public Task InvalidateAsync(CorporateEmail email, EmailCodePurpose purpose,
        string codeHash, CancellationToken cancellationToken) =>
        _codes.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", Key(email, purpose)) &
            Builders<BsonDocument>.Filter.Eq("hash", codeHash),
            Builders<BsonDocument>.Update.Combine(
                Builders<BsonDocument>.Update.Set("expiresAt", DateTime.UnixEpoch),
                Builders<BsonDocument>.Update.Inc("version", 1)),
            cancellationToken: cancellationToken);

    private static string Key(CorporateEmail email, EmailCodePurpose purpose) =>
        $"{purpose}:{email.Value}";

    private async Task EnsureIndexAsync(CancellationToken cancellationToken)
    {
        if (_indexReady) return;
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_indexReady) return;
            await _codes.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("expiresAt"),
                new CreateIndexOptions { Name = "ttl_registration_email_codes", ExpireAfter = TimeSpan.Zero }),
                cancellationToken: cancellationToken);
            _indexReady = true;
        }
        finally { _indexLock.Release(); }
    }
}
