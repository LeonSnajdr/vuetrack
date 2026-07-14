using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;
using Vuetrack.OAuth;

namespace Vuetrack.Backends.Timetracking.Connection;

[Inject]
public class TimetrackingConnectionRepository(ILogger<BaseRepositoryMongo<TimetrackingConnectionModel>> logger, IMongoDbConnector connector)
    : OAuthConnectionRepository<TimetrackingConnectionModel>(logger, connector), ITimetrackingConnectionRepository
{
    public async Task UpsertConnectionAsync(string userId, string authMode, string encryptedRefreshToken, string externalUserId)
    {
        var update = Update
            .Set(x => x.AuthMode, authMode)
            .Set(x => x.EncryptedRefreshToken, encryptedRefreshToken)
            .Set(x => x.ExternalUserId, externalUserId)
            .Set(x => x.Enabled, true)
            .SetOnInsert(x => x.UserId, userId);

        var filter = Filter.Where(x => x.UserId == userId);
        var options = new UpdateOptions { IsUpsert = true };

        await Collection.UpdateOneAsync(filter, update, options);
    }
}

public interface ITimetrackingConnectionRepository : IBaseRepositoryMongo<TimetrackingConnectionModel>
{
    Task<TimetrackingConnectionModel?> GetByUserId(string userId);

    Task UpsertConnectionAsync(string userId, string authMode, string encryptedRefreshToken, string externalUserId);

    Task SetRefreshTokenAsync(string userId, string encryptedRefreshToken);
}
