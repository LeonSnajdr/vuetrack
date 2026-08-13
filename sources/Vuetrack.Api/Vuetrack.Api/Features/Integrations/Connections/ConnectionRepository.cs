using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;

namespace Vuetrack.Api.Features.Integrations.Connections;

[Inject]
public class ConnectionRepository(ILogger<BaseRepositoryMongo<ConnectionModel>> logger, IMongoDbConnector connector) : BaseRepositoryMongo<ConnectionModel>(logger, connector), IConnectionRepository
{
    public async Task<ConnectionModel?> GetAsync(string userId, IntegrationKey key, CancellationToken cancellationToken)
    {
        var filter = BuildFilter(userId, key);
        var found = await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        return found;
    }

    public async Task UpsertAsync(string userId, IntegrationKey key, string encryptedRefreshToken, IReadOnlyDictionary<string, string> attributes, CancellationToken cancellationToken)
    {
        var update = Update
            .Set(x => x.EncryptedRefreshToken, encryptedRefreshToken)
            .Set(x => x.Attributes, attributes.ToDictionary())
            .SetOnInsert(x => x.UserId, userId)
            .SetOnInsert(x => x.Key, key);

        var filter = BuildFilter(userId, key);
        var options = new UpdateOptions { IsUpsert = true };

        await Collection.UpdateOneAsync(filter, update, options, cancellationToken);
    }

    public async Task SetRefreshTokenAsync(string userId, IntegrationKey key, string encryptedRefreshToken, CancellationToken cancellationToken)
    {
        var update = Update.Set(x => x.EncryptedRefreshToken, encryptedRefreshToken);
        var filter = BuildFilter(userId, key);

        await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string userId, IntegrationKey key, CancellationToken cancellationToken)
    {
        var filter = BuildFilter(userId, key);

        await Collection.DeleteOneAsync(filter, cancellationToken);
    }

    private FilterDefinition<ConnectionModel> BuildFilter(string userId, IntegrationKey key)
    {
        var filter = Filter.Where(x => x.UserId == userId && x.Key == key);

        return filter;
    }
}

public interface IConnectionRepository : IBaseRepositoryMongo<ConnectionModel>
{
    Task<ConnectionModel?> GetAsync(string userId, IntegrationKey key, CancellationToken cancellationToken);

    Task UpsertAsync(string userId, IntegrationKey key, string encryptedRefreshToken, IReadOnlyDictionary<string, string> attributes, CancellationToken cancellationToken);

    Task SetRefreshTokenAsync(string userId, IntegrationKey key, string encryptedRefreshToken, CancellationToken cancellationToken);

    Task DeleteAsync(string userId, IntegrationKey key, CancellationToken cancellationToken);
}
