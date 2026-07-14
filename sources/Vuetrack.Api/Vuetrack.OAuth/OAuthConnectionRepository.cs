using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;

namespace Vuetrack.OAuth;

/// <summary>
/// Shared repository behaviour for OAuth connection models: lookup by user and refresh-token rotation.
/// The upsert stays per-platform because the stored fields differ.
/// </summary>
public abstract class OAuthConnectionRepository<TModel>(ILogger<BaseRepositoryMongo<TModel>> logger, IMongoDbConnector connector)
    : BaseRepositoryMongo<TModel>(logger, connector)
    where TModel : OAuthConnectionModel
{
    public async Task<TModel?> GetByUserId(string userId)
    {
        return await Collection.Find(x => x.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task SetRefreshTokenAsync(string userId, string encryptedRefreshToken)
    {
        var update = Update.Set(x => x.EncryptedRefreshToken, encryptedRefreshToken);
        var filter = Filter.Where(x => x.UserId == userId);

        await Collection.UpdateOneAsync(filter, update);
    }
}
