using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;

namespace Vuetrack.Backends.Timetracking.Connection;

[Inject]
public class TimetrackingConnectionRepository(ILogger<TimetrackingConnectionRepository> logger, IMongoDbConnector connector) : BaseRepositoryMongo<TimetrackingConnectionModel>(logger, connector), ITimetrackingConnectionRepository
{
    public async Task<TimetrackingConnectionModel?> GetByUserId(string userId)
    {
        return await Collection.Find(x => x.UserId == userId).FirstOrDefaultAsync();
    }
}

public interface ITimetrackingConnectionRepository : IBaseRepositoryMongo<TimetrackingConnectionModel>
{
    Task<TimetrackingConnectionModel?> GetByUserId(string userId);
}
