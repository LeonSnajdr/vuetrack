using MongoDB.Driver;
using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core;

[Inject]
public class SuggestionRepository : BaseRepositoryMongo<SuggestionModel>, ISuggestionRepository
{
    public SuggestionRepository(ILogger<SuggestionRepository> logger, IMongoDbConnector connector)
        : base(logger, connector)
    {
    }

    public async Task<List<SuggestionModel>> ListAsync(string userId, DateTime from, DateTime to)
    {
        return await Collection
            .Find(x => x.UserId == userId && x.Status != SuggestionStatus.Dismissed && x.Status != SuggestionStatus.Confirmed && x.DateStarted >= from && x.DateStarted < to)
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

    public async Task<SuggestionModel?> UpdateFieldsAsync(string id, string userId, string title, string? taskId, string? projectId, string? activityId, DateTime start, DateTime end, string? comment, DateTime updatedAt)
    {
        var update = Update
            .Set(x => x.Title, title)
            .Set(x => x.TaskId, taskId)
            .Set(x => x.ProjectId, projectId)
            .Set(x => x.ActivityId, activityId)
            .Set(x => x.DateStarted, start)
            .Set(x => x.DateEnded, end)
            .Set(x => x.Comment, comment)
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

    public async Task DeleteResettableAsync(string userId, DateTime from, DateTime to, IReadOnlyList<ConnectorKey>? connectorKeys)
    {
        var resettableStatuses = new[] { SuggestionStatus.Pending, SuggestionStatus.Edited, SuggestionStatus.Dismissed };
        var filter = Filter.Where(x => x.UserId == userId && x.DateStarted >= from && x.DateStarted < to && resettableStatuses.Contains(x.Status));

        if (connectorKeys is not null)
        {
            filter &= Filter.Where(x => x.Sources.Any(s => connectorKeys.Contains(s.ConnectorKey)));
        }

        await Collection.DeleteManyAsync(filter);
    }
}

public interface ISuggestionRepository : IBaseRepositoryMongo<SuggestionModel>
{
    Task<List<SuggestionModel>> ListAsync(string userId, DateTime from, DateTime to);

    Task InsertManyAsync(IReadOnlyList<SuggestionModel> items);

    Task<bool> ExistsBySourceAsync(string userId, ConnectorKey connectorKey, string externalId);

    Task<SuggestionModel?> UpdateFieldsAsync(string id, string userId, string title, string? taskId, string? projectId, string? activityId, DateTime start, DateTime end, string? comment, DateTime updatedAt);

    Task<bool> SetStatusAsync(string id, string userId, SuggestionStatus status, DateTime updatedAt);

    Task DeleteResettableAsync(string userId, DateTime from, DateTime to, IReadOnlyList<ConnectorKey>? connectorKeys);
}
