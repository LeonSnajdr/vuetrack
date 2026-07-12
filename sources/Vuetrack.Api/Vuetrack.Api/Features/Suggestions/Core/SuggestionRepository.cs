using MongoDB.Driver;
using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core;

[Inject]
public class SuggestionRepository : BaseRepositoryMongo<SuggestionModel>, ISuggestionRepository
{
    // First compound index in this codebase - Samhammer.Mongo has no established index-creation
    // convention, so we ensure it once per process here rather than inventing a repo-wide pattern.
    private static int indexEnsured;

    private readonly ILogger<SuggestionRepository> logger;

    public SuggestionRepository(ILogger<SuggestionRepository> logger, IMongoDbConnector connector)
        : base(logger, connector)
    {
        this.logger = logger;
        EnsureIndexes();
    }

    public async Task<List<SuggestionModel>> ListAsync(string userId, DateTime from, DateTime to)
    {
        return await Collection
            .Find(x => x.UserId == userId && x.Status != SuggestionStatus.Dismissed && x.DateStarted >= from && x.DateStarted < to)
            .SortBy(x => x.DateStarted)
            .ToListAsync();
    }

    public async Task InsertManyAsync(IReadOnlyList<SuggestionModel> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        await Collection.InsertManyAsync(items);
    }

    public async Task<bool> ExistsBySourceAsync(string userId, ConnectorKey connectorKey, string externalId)
    {
        return await Collection
            .Find(x => x.UserId == userId && x.Sources.Any(s => s.ConnectorKey == connectorKey && s.ExternalId == externalId))
            .AnyAsync();
    }

    public async Task<SuggestionModel?> UpdateFieldsAsync(string id, string userId, string title, string? description, DateTime start, DateTime end, DateTime updatedAt)
    {
        var update = Update
            .Set(x => x.Title, title)
            .Set(x => x.Description, description)
            .Set(x => x.DateStarted, start)
            .Set(x => x.DateEnded, end)
            .Set(x => x.Status, SuggestionStatus.Edited)
            .Set(x => x.DateUpdated, updatedAt);

        var filters = new List<FilterDefinition<SuggestionModel>>
        {
            Filter.Where(x => x.Id == id),
            Filter.Where(x => x.UserId == userId),
        };

        return await Collection.FindOneAndUpdateAsync(
            Filter.And(filters),
            update,
            new FindOneAndUpdateOptions<SuggestionModel> { ReturnDocument = ReturnDocument.After });
    }

    public async Task<bool> SetStatusAsync(string id, string userId, SuggestionStatus status, DateTime updatedAt)
    {
        var update = Update
            .Set(x => x.Status, status)
            .Set(x => x.DateUpdated, updatedAt);

        var filters = new List<FilterDefinition<SuggestionModel>>
        {
            Filter.Where(x => x.Id == id),
            Filter.Where(x => x.UserId == userId),
        };

        var result = await Collection.UpdateOneAsync(Filter.And(filters), update);

        return result.MatchedCount > 0;
    }

    private void EnsureIndexes()
    {
        if (Interlocked.CompareExchange(ref indexEnsured, 1, 0) != 0)
        {
            return;
        }

        var indexKeys = Builders<SuggestionModel>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.DateStarted);

        _ = Collection.Indexes.CreateOneAsync(new CreateIndexModel<SuggestionModel>(indexKeys))
            .ContinueWith(t => logger.LogWarning(t.Exception, "Failed to ensure suggestions index"), TaskContinuationOptions.OnlyOnFaulted);
    }
}

public interface ISuggestionRepository : IBaseRepositoryMongo<SuggestionModel>
{
    Task<List<SuggestionModel>> ListAsync(string userId, DateTime from, DateTime to);

    Task InsertManyAsync(IReadOnlyList<SuggestionModel> items);

    Task<bool> ExistsBySourceAsync(string userId, ConnectorKey connectorKey, string externalId);

    Task<SuggestionModel?> UpdateFieldsAsync(string id, string userId, string title, string? description, DateTime start, DateTime end, DateTime updatedAt);

    Task<bool> SetStatusAsync(string id, string userId, SuggestionStatus status, DateTime updatedAt);
}
