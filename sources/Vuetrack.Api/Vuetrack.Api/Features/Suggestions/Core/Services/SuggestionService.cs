using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Connectors;
using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Features.Suggestions.Engine;
using Vuetrack.Api.Features.TimeEntry.Services;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core.Services;

[Inject]
public class SuggestionService(IConnectorResolver resolver, ISuggestionRepository repository, ISuggestionEngine engine, ITimeEntryService timeEntryService, ILogger<SuggestionService> logger) : ISuggestionService
{
    private IConnectorResolver Resolver { get; } = resolver;

    private ISuggestionRepository Repository { get; } = repository;

    private ISuggestionEngine Engine { get; } = engine;

    private ITimeEntryService TimeEntryService { get; } = timeEntryService;

    private ILogger<SuggestionService> Logger { get; } = logger;

    public async Task<ErrorOr<IReadOnlyList<SuggestionContract>>> GenerateAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken)
    {
        var connectors = await Resolver.ResolveAllConnectedAsync(userId, cancellationToken);
        var fetched = await FetchAllAsync(connectors, userId, request.From, request.To, cancellationToken);

        var inserted = await BuildAndInsertAsync(userId, request.From, request.To, fetched.Signals, cancellationToken);
        return inserted;
    }

    public async Task<ErrorOr<IReadOnlyList<SuggestionContract>>> ReloadAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken)
    {
        var connectors = await Resolver.ResolveAllConnectedAsync(userId, cancellationToken);
        var fetched = await FetchAllAsync(connectors, userId, request.From, request.To, cancellationToken);

        if (fetched.SuccessfulKeys.Count > 0)
        {
            await Repository.DeleteResettableAsync(userId, request.From, request.To, fetched.SuccessfulKeys);
        }

        var inserted = await BuildAndInsertAsync(userId, request.From, request.To, fetched.Signals, cancellationToken);
        return inserted;
    }

    private async Task<(List<ActivitySignal> Signals, List<ConnectorKey> SuccessfulKeys)> FetchAllAsync(IReadOnlyList<IConnector> connectors, string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var fetchTasks = connectors.Select(async connector =>
        {
            var fetched = await FetchFromConnectorAsync(connector, userId, from, to, cancellationToken);
            return (connector.Descriptor.Key, Signals: fetched);
        });

        var results = await Task.WhenAll(fetchTasks);

        var signals = new List<ActivitySignal>();
        var successfulKeys = new List<ConnectorKey>();
        foreach (var result in results)
        {
            if (result.Signals.IsError)
            {
                continue;
            }

            successfulKeys.Add(result.Key);
            signals.AddRange(result.Signals.Value);
        }

        return (signals, successfulKeys);
    }

    public async Task<IReadOnlyList<SuggestionContract>> ListAsync(string userId, DateTime from, DateTime to)
    {
        var models = await Repository.ListAsync(userId, from, to);
        return models.Select(m => m.ToContract()).ToList();
    }

    public async Task<ErrorOr<SuggestionContract>> UpdateAsync(string userId, string id, SuggestionUpdateContract request, CancellationToken cancellationToken)
    {
        var updated = await Repository.UpdateFieldsAsync(
            id,
            userId,
            request.TaskId,
            request.ProjectId,
            request.ActivityId,
            request.DateStarted,
            request.DateEnded,
            request.Comment,
            DateTime.UtcNow);

        if (updated is null)
        {
            return Error.NotFound();
        }

        return updated.ToContract();
    }

    public async Task<ErrorOr<Deleted>> DismissAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var found = await Repository.SetStatusAsync(id, userId, SuggestionStatus.Dismissed, DateTime.UtcNow);

        if (!found)
        {
            return Error.NotFound();
        }

        return Result.Deleted;
    }

    public async Task<ErrorOr<Success>> AcceptAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var found = await Repository.SetStatusAsync(id, userId, SuggestionStatus.Confirmed, DateTime.UtcNow);

        if (!found)
        {
            return Error.NotFound();
        }

        return Result.Success;
    }

    private async Task<ErrorOr<IReadOnlyList<SuggestionContract>>> BuildAndInsertAsync(string userId, DateTime from, DateTime to, IReadOnlyList<ActivitySignal> signals, CancellationToken cancellationToken)
    {
        var existingTaskEntries = await GetExistingTaskEntriesAsync(userId, from, to, cancellationToken);
        if (existingTaskEntries.IsError)
        {
            return existingTaskEntries.Errors;
        }

        var built = await Engine.BuildAsync(signals, from, to, cancellationToken);
        if (built.IsError)
        {
            Logger.LogWarning("Suggestion engine failed to build suggestions for user {UserId}: {Errors}", userId, built.Errors);
            return built.Errors;
        }

        var suggestions = built.Value;
        var existingEntries = existingTaskEntries.Value;
        var now = DateTime.UtcNow;
        var toInsert = new List<SuggestionModel>();

        var candidateExternalIds = suggestions.SelectMany(s => s.Sources).Select(s => s.ExternalId).Distinct().ToList();
        var existingSources = await Repository.GetSourcesByExternalIdsAsync(userId, candidateExternalIds);
        var existingSourceKeys = existingSources.Select(s => (s.ConnectorKey, s.ExternalId)).ToHashSet();

        foreach (var suggestion in suggestions)
        {
            var taskAlreadyExists = suggestion.TaskId is not null
                && existingEntries.TryGetValue(suggestion.TaskId, out var existingRanges)
                && existingRanges.Any(r => r.Start < suggestion.DateEnded && suggestion.DateStarted < r.End);
            if (taskAlreadyExists)
            {
                continue;
            }

            var isAlreadyGenerated = suggestion.Sources.Any(source => existingSourceKeys.Contains((source.ConnectorKey, source.ExternalId)));
            if (isAlreadyGenerated)
            {
                continue;
            }

            var suggestionModel = suggestion.ToModel(userId, now);
            toInsert.Add(suggestionModel);
        }

        await Repository.InsertManyAsync(toInsert);

        IReadOnlyList<SuggestionContract> contracts = toInsert.Select(m => m.ToContract()).ToList();
        return contracts.ToErrorOr();
    }

    private async Task<ErrorOr<Dictionary<string, List<(DateTime Start, DateTime End)>>>> GetExistingTaskEntriesAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var entries = await TimeEntryService.ListAsync(userId, from, to, cancellationToken);
        if (entries.IsError)
        {
            Logger.LogWarning("Could not load time entries for suggestion deduplication for user {UserId}: {Errors}", userId, entries.Errors);
            return entries.Errors;
        }

        var byTask = entries.Value
            .Where(x => x.TaskId is { Length: > 0 })
            .GroupBy(x => x.TaskId!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(x => (x.DateStarted, x.DateEnded)).ToList(), StringComparer.Ordinal);

        return byTask;
    }

    private async Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchFromConnectorAsync(IConnector connector, string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var key = connector.Descriptor.Key;

        try
        {
            var container = new ActivityFetchContainer { From = from, To = to };
            var fetch = await connector.FetchAsync(container, cancellationToken);
            if (fetch.IsError)
            {
                Logger.LogWarning("Connector {ConnectorKey} failed to fetch signals for user {UserId}: {Error}", key, userId, fetch.FirstError.Description);
                return fetch.Errors;
            }

            return fetch.Value.ToErrorOr();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "Connector {ConnectorKey} threw while fetching suggestion signals for user {UserId}", key, userId);
            return Error.Failure(description: "Connector threw while fetching signals.");
        }
    }
}

public interface ISuggestionService
{
    Task<ErrorOr<IReadOnlyList<SuggestionContract>>> GenerateAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<SuggestionContract>>> ReloadAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken);

    Task<IReadOnlyList<SuggestionContract>> ListAsync(string userId, DateTime from, DateTime to);

    Task<ErrorOr<SuggestionContract>> UpdateAsync(string userId, string id, SuggestionUpdateContract request, CancellationToken cancellationToken);

    Task<ErrorOr<Deleted>> DismissAsync(string userId, string id, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> AcceptAsync(string userId, string id, CancellationToken cancellationToken);
}
