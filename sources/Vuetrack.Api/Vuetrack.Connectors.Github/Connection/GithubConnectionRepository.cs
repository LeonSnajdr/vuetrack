using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;
using Vuetrack.OAuth;

namespace Vuetrack.Connectors.Github.Connection;

[Inject]
public class GithubConnectionRepository(ILogger<BaseRepositoryMongo<GithubConnectionModel>> logger, IMongoDbConnector connector) : OAuthConnectionRepository<GithubConnectionModel>(logger, connector), IGithubConnectionRepository
{
    public async Task UpsertConnectionAsync(string userId, string login, string authMode, string encryptedRefreshToken)
    {
        var update = Update
            .Set(x => x.Login, login)
            .Set(x => x.AuthMode, authMode)
            .Set(x => x.EncryptedRefreshToken, encryptedRefreshToken)
            .Set(x => x.Enabled, true)
            .SetOnInsert(x => x.UserId, userId);

        var filter = Filter.Where(x => x.UserId == userId);
        var options = new UpdateOptions { IsUpsert = true };

        await Collection.UpdateOneAsync(filter, update, options);
    }
}

public interface IGithubConnectionRepository : IOAuthConnectionRepository<GithubConnectionModel>
{
    Task UpsertConnectionAsync(string userId, string login, string authMode, string encryptedRefreshToken);
}
