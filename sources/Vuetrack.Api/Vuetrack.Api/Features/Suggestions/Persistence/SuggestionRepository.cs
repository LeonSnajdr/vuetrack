using MongoDB.Driver;
using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Persistence;

[Inject]
public class SuggestionRepository(ILogger<BaseRepositoryMongo<SuggestionModel>> logger, IMongoDbConnector connector) : BaseRepositoryMongo<SuggestionModel>(logger, connector), ISuggestionRepository
{
    public async Task<List<SuggestionModel>> ListAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        return await Collection
            .Find(x => x.UserId == userId && x.Status != SuggestionStatus.Dismissed && x.Status != SuggestionStatus.Confirmed && x.DateStarted >= from && x.DateStarted < to)
            .SortBy(x => x.DateStarted)
            .ToListAsync(cancellationToken);
    }

    public async Task InsertManyAsync(IReadOnlyList<SuggestionModel> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        await Collection.InsertManyAsync(items, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SuggestionEvidenceModel>> GetSourcesByExternalIdsAsync(string userId, IReadOnlyList<string> externalIds, CancellationToken cancellationToken)
    {
        if (externalIds.Count == 0)
        {
            return [];
        }

        var sourceLists = await Collection
            .Find(x => x.UserId == userId && x.Sources.Any(s => externalIds.Contains(s.ExternalId)))
            .Project(x => x.Sources)
            .ToListAsync(cancellationToken);

        return sourceLists.SelectMany(sources => sources).ToList();
    }

    public async Task<SuggestionModel?> UpdateFieldsAsync(string id, string userId, string? taskId, string? projectId, string? activityId, DateTime start, DateTime end, string? comment, DateTime updatedAt, CancellationToken cancellationToken)
    {
        var update = Update
            .Set(x => x.TaskId, taskId)
            .Set(x => x.ProjectId, projectId)
            .Set(x => x.ActivityId, activityId)
            .Set(x => x.DateStarted, start)
            .Set(x => x.DateEnded, end)
            .Set(x => x.Comment, comment)
            .Set(x => x.Status, SuggestionStatus.Edited)
            .Set(x => x.DateUpdated, updatedAt);

        var filter = BuildFilter(id, userId);
        var options = new FindOneAndUpdateOptions<SuggestionModel> { ReturnDocument = ReturnDocument.After };

        return await Collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
    }

    public async Task<bool> SetStatusAsync(string id, string userId, SuggestionStatus status, DateTime updatedAt, CancellationToken cancellationToken)
    {
        var update = Update
            .Set(x => x.Status, status)
            .Set(x => x.DateUpdated, updatedAt);

        var filter = BuildFilter(id, userId);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        return result.MatchedCount > 0;
    }

    public async Task DeleteResettableAsync(string userId, DateTime from, DateTime to, IReadOnlyList<IntegrationKey>? keys, CancellationToken cancellationToken)
    {
        var resettableStatuses = new[] { SuggestionStatus.Pending, SuggestionStatus.Edited, SuggestionStatus.Dismissed };
        var filter = Filter.Where(x => x.UserId == userId && x.DateStarted >= from && x.DateStarted < to && resettableStatuses.Contains(x.Status));

        if (keys is not null)
        {
            filter &= Filter.Where(x => x.Sources.Any(s => keys.Contains(s.Key)));
        }

        await Collection.DeleteManyAsync(filter, cancellationToken);
    }

    private FilterDefinition<SuggestionModel> BuildFilter(string id, string userId)
    {
        var filter = Filter.Where(x => x.Id == id && x.UserId == userId);

        return filter;
    }
}

public interface ISuggestionRepository : IBaseRepositoryMongo<SuggestionModel>
{
    Task<List<SuggestionModel>> ListAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken);

    Task InsertManyAsync(IReadOnlyList<SuggestionModel> items, CancellationToken cancellationToken);

    Task<IReadOnlyList<SuggestionEvidenceModel>> GetSourcesByExternalIdsAsync(string userId, IReadOnlyList<string> externalIds, CancellationToken cancellationToken);

    Task<SuggestionModel?> UpdateFieldsAsync(string id, string userId, string? taskId, string? projectId, string? activityId, DateTime start, DateTime end, string? comment, DateTime updatedAt, CancellationToken cancellationToken);

    Task<bool> SetStatusAsync(string id, string userId, SuggestionStatus status, DateTime updatedAt, CancellationToken cancellationToken);

    Task DeleteResettableAsync(string userId, DateTime from, DateTime to, IReadOnlyList<IntegrationKey>? keys, CancellationToken cancellationToken);
}
