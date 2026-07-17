using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;

namespace Vuetrack.OAuth;

public abstract class OAuthConnectionRepository<TModel>(ILogger<BaseRepositoryMongo<TModel>> logger, IMongoDbConnector connector) : BaseRepositoryMongo<TModel>(logger, connector), IOAuthConnectionRepository<TModel> where TModel : OAuthConnectionModel
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

public interface IOAuthConnectionRepository<TModel> : IBaseRepositoryMongo<TModel> where TModel : OAuthConnectionModel
{
    Task<TModel?> GetByUserId(string userId);

    Task SetRefreshTokenAsync(string userId, string encryptedRefreshToken);
}
