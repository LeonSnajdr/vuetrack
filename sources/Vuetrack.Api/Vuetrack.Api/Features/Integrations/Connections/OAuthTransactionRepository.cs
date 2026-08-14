using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Integrations.Connections;

[Inject]
public class OAuthTransactionRepository(ILogger<BaseRepositoryMongo<OAuthTransactionModel>> logger, IMongoDbConnector connector)
    : BaseRepositoryMongo<OAuthTransactionModel>(logger, connector), IOAuthTransactionRepository
{
    public async Task CreateAsync(OAuthTransactionModel transaction, CancellationToken cancellationToken)
    {
        await Collection.InsertOneAsync(transaction, cancellationToken: cancellationToken);
    }

    public async Task<OAuthTransactionModel?> ConsumeAsync(
        string state,
        string userId,
        IntegrationKey key,
        string redirectUri,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var filter = Filter.Where(x =>
            x.State == state &&
            x.UserId == userId &&
            x.Key == key &&
            x.RedirectUri == redirectUri &&
            x.DateExpires > now);

        return await Collection.FindOneAndDeleteAsync(filter, cancellationToken: cancellationToken);
    }
}

public interface IOAuthTransactionRepository : IBaseRepositoryMongo<OAuthTransactionModel>
{
    Task CreateAsync(OAuthTransactionModel transaction, CancellationToken cancellationToken);

    Task<OAuthTransactionModel?> ConsumeAsync(
        string state,
        string userId,
        IntegrationKey key,
        string redirectUri,
        DateTime now,
        CancellationToken cancellationToken);
}
