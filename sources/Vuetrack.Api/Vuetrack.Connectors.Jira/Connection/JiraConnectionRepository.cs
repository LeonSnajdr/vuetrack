using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;

namespace Vuetrack.Connectors.Jira.Connection;

[Inject]
public class JiraConnectionRepository(ILogger<JiraConnectionRepository> logger, IMongoDbConnector connector) : BaseRepositoryMongo<JiraConnectionModel>(logger, connector), IJiraConnectionRepository
{
    public async Task<JiraConnectionModel?> GetByUserId(string userId)
    {
        return await Collection.Find(x => x.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task UpsertConnectionAsync(string userId, string siteUrl, string cloudId, string authMode, string encryptedRefreshToken)
    {
        var update = Update
            .Set(x => x.SiteUrl, siteUrl)
            .Set(x => x.CloudId, cloudId)
            .Set(x => x.AuthMode, authMode)
            .Set(x => x.EncryptedRefreshToken, encryptedRefreshToken)
            .Set(x => x.Enabled, true)
            .SetOnInsert(x => x.UserId, userId);

        var filter = Filter.Where(x => x.UserId == userId);
        var options = new UpdateOptions { IsUpsert = true };

        await Collection.UpdateOneAsync(filter, update, options);
    }

    public async Task SetRefreshTokenAsync(string userId, string encryptedRefreshToken)
    {
        var update = Update.Set(x => x.EncryptedRefreshToken, encryptedRefreshToken);
        var filter = Filter.Where(x => x.UserId == userId);

        await Collection.UpdateOneAsync(filter, update);
    }
}

public interface IJiraConnectionRepository : IBaseRepositoryMongo<JiraConnectionModel>
{
    Task<JiraConnectionModel?> GetByUserId(string userId);

    Task UpsertConnectionAsync(string userId, string siteUrl, string cloudId, string authMode, string encryptedRefreshToken);

    Task SetRefreshTokenAsync(string userId, string encryptedRefreshToken);
}
