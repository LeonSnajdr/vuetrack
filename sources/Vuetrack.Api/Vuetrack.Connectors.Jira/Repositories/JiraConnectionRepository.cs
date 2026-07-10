using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;
using Vuetrack.Connectors.Jira.Models;

namespace Vuetrack.Connectors.Jira.Repositories;

[Inject]
public class JiraConnectionRepository(ILogger<JiraConnectionRepository> logger, IMongoDbConnector connector) : BaseRepositoryMongo<JiraConnectionModel>(logger, connector), IJiraConnectionRepository
{
    public async Task<JiraConnectionModel?> GetByUserId(string userId)
    {
        return await Collection.Find(x => x.UserId == userId).FirstOrDefaultAsync();
    }
}

public interface IJiraConnectionRepository : IBaseRepositoryMongo<JiraConnectionModel>
{
    Task<JiraConnectionModel?> GetByUserId(string userId);
}
