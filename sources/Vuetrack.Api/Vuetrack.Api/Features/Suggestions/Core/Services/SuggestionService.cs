using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Connectors;
using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Features.Suggestions.Engine;
using Vuetrack.Api.Features.TimeEntry.Services;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core.Services;

[Inject]
public class SuggestionService(IConnectorRegistry registry, IConnectorResolver resolver, ISuggestionRepository repository, ISuggestionEngine engine, ITimeEntryService timeEntryService, ILogger<SuggestionService> logger) : ISuggestionService
{
    private IConnectorRegistry Registry { get; } = registry;

    private IConnectorResolver Resolver { get; } = resolver;

    private ISuggestionRepository Repository { get; } = repository;

    private ISuggestionEngine Engine { get; } = engine;

    private ITimeEntryService TimeEntryService { get; } = timeEntryService;

    private ILogger<SuggestionService> Logger { get; } = logger;

    public async Task<ErrorOr<IReadOnlyList<SuggestionContract>>> GenerateAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken)
    {
        var descriptors = SelectDescriptors(request);
        var fetched = await FetchAllAsync(descriptors, userId, request.From, request.To, cancellationToken);

        var inserted = await BuildAndInsertAsync(userId, request.From, request.To, fetched.Signals, cancellationToken);
        return inserted;
    }

    public async Task<ErrorOr<IReadOnlyList<SuggestionContract>>> ReloadAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken)
    {
        var descriptors = SelectDescriptors(request);
        var fetched = await FetchAllAsync(descriptors, userId, request.From, request.To, cancellationToken);

        if (fetched.SuccessfulKeys.Count > 0)
        {
            await Repository.DeleteResettableAsync(userId, request.From, request.To, fetched.SuccessfulKeys);
        }

        var inserted = await BuildAndInsertAsync(userId, request.From, request.To, fetched.Signals, cancellationToken);
        return inserted;
    }

    // Connectors are independent (each resolves its own connection), so fetch them concurrently.
    private async Task<(List<ActivitySignal> Signals, List<ConnectorKey> SuccessfulKeys)> FetchAllAsync(List<ConnectorDescriptor> descriptors, string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var fetchTasks = descriptors.Select(async descriptor =>
        {
            var fetched = await FetchFromConnectorAsync(descriptor, userId, from, to, cancellationToken);
            return (descriptor.Key, Signals: fetched);
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

    private List<ConnectorDescriptor> SelectDescriptors(GenerateSuggestionsRequestContract request)
    {
        var descriptors = Registry.Descriptors
            .Where(d => request.ConnectorKeys is null || request.ConnectorKeys.Contains(d.Key))
            .ToList();

        return descriptors;
    }

    private async Task<ErrorOr<IReadOnlyList<SuggestionContract>>> BuildAndInsertAsync(string userId, DateTime from, DateTime to, IReadOnlyList<ActivitySignal> signals, CancellationToken cancellationToken)
    {
        var existingTaskIds = await GetExistingTaskIdsAsync(userId, from, to, cancellationToken);
        if (existingTaskIds.IsError)
        {
            return existingTaskIds.Errors;
        }

        var built = await Engine.BuildAsync(signals, from, to, cancellationToken);
        if (built.IsError)
        {
            Logger.LogWarning("Suggestion engine failed to build suggestions for user {UserId}: {Errors}", userId, built.Errors);
            return built.Errors;
        }

        var suggestions = built.Value;
        var existingIds = existingTaskIds.Value;
        var now = DateTime.UtcNow;
        var toInsert = new List<SuggestionModel>();

        var candidateExternalIds = suggestions.SelectMany(s => s.Sources).Select(s => s.ExternalId).Distinct().ToList();
        var existingSources = await Repository.GetSourcesByExternalIdsAsync(userId, candidateExternalIds);
        var existingSourceKeys = existingSources.Select(s => (s.ConnectorKey, s.ExternalId)).ToHashSet();

        foreach (var suggestion in suggestions)
        {
            // TODO: Check For whole week? should be just for this day
            var taskAlreadyExists = suggestion.TaskId is not null && existingIds.Contains(suggestion.TaskId);
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

    private async Task<ErrorOr<HashSet<string>>> GetExistingTaskIdsAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var entries = await TimeEntryService.ListAsync(userId, from, to, cancellationToken);
        if (entries.IsError)
        {
            Logger.LogWarning("Could not load time entries for suggestion deduplication for user {UserId}: {Errors}", userId, entries.Errors);
            return entries.Errors;
        }

        var taskIds = entries.Value
            .Select(x => x.TaskId)
            .OfType<string>()
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        return taskIds;
    }

    private async Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchFromConnectorAsync(ConnectorDescriptor descriptor, string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        try
        {
            var resolved = await Resolver.ResolveConnectedAsync(descriptor.Key, userId, cancellationToken);
            if (resolved.IsError)
            {
                Logger.LogInformation("Connector {ConnectorKey} unavailable for user {UserId}: {Error}", descriptor.Key, userId, resolved.FirstError.Description);
                return resolved.Errors;
            }

            var container = new ActivityFetchContainer { From = from, To = to };
            var fetch = await resolved.Value.FetchAsync(container, cancellationToken);
            if (fetch.IsError)
            {
                Logger.LogWarning("Connector {ConnectorKey} failed to fetch signals for user {UserId}: {Error}", descriptor.Key, userId, fetch.FirstError.Description);
                return fetch.Errors;
            }

            return fetch.Value.ToErrorOr();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "Connector {ConnectorKey} threw while fetching suggestion signals for user {UserId}", descriptor.Key, userId);
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
