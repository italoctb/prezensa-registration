using MongoDB.Bson;
using MongoDB.Driver;
using Registration.Application.Abstractions.Persistence;
using Registration.Application.Accounts;
using Registration.Domain.Drafts;

namespace Registration.Infrastructure.Persistence.Mongo;

public sealed class MongoAccountRepository(IMongoDatabase database) : IAccountRepository
{
    private readonly IMongoCollection<BsonDocument> _accounts = database.GetCollection<BsonDocument>("registration_accounts");

    public async Task<UserAccount?> FindByEmailAsync(CorporateEmail email, CancellationToken cancellationToken)
    {
        var document = await _accounts.Find(Builders<BsonDocument>.Filter.Eq("_id", email.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : new UserAccount(
            OwnerId.From(document["ownerId"].AsGuid), email,
            new DateTimeOffset(document["createdAt"].ToUniversalTime()));
    }

    public async Task<bool> TryCreateAsync(UserAccount account, CancellationToken cancellationToken)
    {
        var document = new BsonDocument
        {
            ["_id"] = account.Email.Value,
            ["ownerId"] = new BsonBinaryData(account.Id.Value, GuidRepresentation.Standard),
            ["createdAt"] = account.CreatedAt.UtcDateTime
        };
        try
        {
            await _accounts.InsertOneAsync(document, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}
