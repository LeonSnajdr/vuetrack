using Vuetrack.Api.Features.Suggestions;

namespace Vuetrack.Api.Tests.Fakes;

public sealed class FakeSuggestionRepository : ISuggestionRepository
{
    private readonly List<SuggestionModel> items = [];

    public IReadOnlyList<SuggestionModel> Items => items;

    public Task<List<SuggestionModel>> ListAsync(string userId, DateTimeOffset from, DateTimeOffset to)
    {
        var result = items
            .Where(x => x.UserId == userId && x.Status != SuggestionStatus.Dismissed && x.Start >= from && x.Start < to)
            .OrderBy(x => x.Start)
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

    public Task<bool> ExistsBySourceAsync(string userId, string connectorKey, string externalId)
    {
        var exists = items.Any(x => x.UserId == userId && x.Sources.Any(s => s.ConnectorKey == connectorKey && s.ExternalId == externalId));
        return Task.FromResult(exists);
    }

    public Task<SuggestionModel?> UpdateFieldsAsync(string id, string userId, string title, string? description, DateTimeOffset start, DateTimeOffset end, DateTimeOffset updatedAt)
    {
        var model = items.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (model is null)
        {
            return Task.FromResult<SuggestionModel?>(null);
        }

        model.Title = title;
        model.Description = description;
        model.Start = start;
        model.End = end;
        model.Status = SuggestionStatus.Edited;
        model.UpdatedAt = updatedAt;

        return Task.FromResult<SuggestionModel?>(model);
    }

    public Task<bool> SetStatusAsync(string id, string userId, SuggestionStatus status, DateTimeOffset updatedAt)
    {
        var model = items.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (model is null)
        {
            return Task.FromResult(false);
        }

        model.Status = status;
        model.UpdatedAt = updatedAt;

        return Task.FromResult(true);
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
