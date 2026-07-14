using Vuetrack.Api.Features.Suggestions.Core;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeSuggestionRepository : ISuggestionRepository
{
    private readonly List<SuggestionModel> items = [];

    public IReadOnlyList<SuggestionModel> Items => items;

    public Task<List<SuggestionModel>> ListAsync(string userId, DateTime from, DateTime to)
    {
        var result = items
            .Where(x => x.UserId == userId && x.Status != SuggestionStatus.Dismissed && x.Status != SuggestionStatus.Confirmed && x.DateStarted >= from && x.DateStarted < to)
            .OrderBy(x => x.DateStarted)
            .ToList();

        return Task.FromResult(result);
    }

    public Task InsertManyAsync(IReadOnlyList<SuggestionModel> newItems)
    {
        foreach (var item in newItems)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = Guid.NewGuid().ToString();
            }

            items.Add(item);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsBySourceAsync(string userId, ConnectorKey connectorKey, string externalId)
    {
        var exists = items.Any(x => x.UserId == userId && x.Sources.Any(s => s.ConnectorKey == connectorKey && s.ExternalId == externalId));
        return Task.FromResult(exists);
    }

    public Task<SuggestionModel?> UpdateFieldsAsync(string id, string userId, string title, string? taskId, string? projectId, string? activityId, DateTime start, DateTime end, string? comment, DateTime updatedAt)
    {
        var model = items.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (model is null)
        {
            return Task.FromResult<SuggestionModel?>(null);
        }

        model.Title = title;
        model.TaskId = taskId;
        model.ProjectId = projectId;
        model.ActivityId = activityId;
        model.DateStarted = start;
        model.DateEnded = end;
        model.Comment = comment;
        model.Status = SuggestionStatus.Edited;
        model.DateUpdated = updatedAt;

        return Task.FromResult<SuggestionModel?>(model);
    }

    public Task<bool> SetStatusAsync(string id, string userId, SuggestionStatus status, DateTime updatedAt)
    {
        var model = items.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (model is null)
        {
            return Task.FromResult(false);
        }

        model.Status = status;
        model.DateUpdated = updatedAt;

        return Task.FromResult(true);
    }

    public Task DeleteResettableAsync(string userId, DateTime from, DateTime to, IReadOnlyList<ConnectorKey>? connectorKeys)
    {
        items.RemoveAll(x =>
            x.UserId == userId &&
            x.DateStarted >= from &&
            x.DateStarted < to &&
            x.Status is SuggestionStatus.Pending or SuggestionStatus.Edited or SuggestionStatus.Dismissed &&
            (connectorKeys is null || x.Sources.Any(s => connectorKeys.Contains(s.ConnectorKey))));

        return Task.CompletedTask;
    }

    public Task<SuggestionModel> GetById(string id) => Task.FromResult(items.First(x => x.Id == id));

    public Task<List<SuggestionModel>> GetAll() => Task.FromResult(items.ToList());

    public Task Save(SuggestionModel model)
    {
        var index = items.FindIndex(x => x.Id == model.Id);
        if (index >= 0)
        {
            items[index] = model;
            return Task.CompletedTask;
        }

        if (string.IsNullOrEmpty(model.Id))
        {
            model.Id = Guid.NewGuid().ToString();
        }

        items.Add(model);
        return Task.CompletedTask;
    }

    public Task Create(SuggestionModel model)
    {
        if (string.IsNullOrEmpty(model.Id))
        {
            model.Id = Guid.NewGuid().ToString();
        }

        items.Add(model);
        return Task.CompletedTask;
    }

    public Task Delete(SuggestionModel model)
    {
        items.RemoveAll(x => x.Id == model.Id);
        return Task.CompletedTask;
    }

    public Task DeleteById(string id)
    {
        items.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task DeleteAll()
    {
        items.Clear();
        return Task.CompletedTask;
    }
}
