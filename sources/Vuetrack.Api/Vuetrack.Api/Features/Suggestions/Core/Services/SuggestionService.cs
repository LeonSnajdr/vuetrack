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

    public async Task<GenerateSuggestionsResultContract> GenerateAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken)
    {
        var descriptors = Registry.Descriptors
            .Where(d => request.ConnectorKeys is null || request.ConnectorKeys.Contains(d.Key))
            .ToList();

        var signals = new List<ActivitySignal>();
        var outcomes = new List<ConnectorOutcomeContract>();

        foreach (var descriptor in descriptors)
        {
            var (outcome, fetchedSignals) = await FetchFromConnectorAsync(descriptor, userId, request.From, request.To, cancellationToken);
            outcomes.Add(outcome);

            if (fetchedSignals is not null)
            {
                signals.AddRange(fetchedSignals);
            }
        }

        var generatedCount = await BuildAndInsertAsync(userId, request.From, request.To, signals, cancellationToken);
        return new GenerateSuggestionsResultContract(generatedCount, outcomes);
    }

    public async Task<GenerateSuggestionsResultContract> ReloadAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken)
    {
        var descriptors = Registry.Descriptors
            .Where(d => request.ConnectorKeys is null || request.ConnectorKeys.Contains(d.Key))
            .ToList();

        var signals = new List<ActivitySignal>();
        var outcomes = new List<ConnectorOutcomeContract>();
        var successfulConnectorKeys = new List<ConnectorKey>();

        foreach (var descriptor in descriptors)
        {
            var (outcome, fetchedSignals) = await FetchFromConnectorAsync(descriptor, userId, request.From, request.To, cancellationToken);
            outcomes.Add(outcome);

            if (fetchedSignals is not null)
            {
                successfulConnectorKeys.Add(descriptor.Key);
                signals.AddRange(fetchedSignals);
            }
        }

        if (successfulConnectorKeys.Count > 0)
        {
            await Repository.DeleteResettableAsync(userId, request.From, request.To, successfulConnectorKeys);
        }

        var generatedCount = await BuildAndInsertAsync(userId, request.From, request.To, signals, cancellationToken);
        return new GenerateSuggestionsResultContract(generatedCount, outcomes);
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
            request.Title,
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

    private async Task<int> BuildAndInsertAsync(string userId, DateTime from, DateTime to, IReadOnlyList<ActivitySignal> signals, CancellationToken cancellationToken)
    {
        var existingTaskIds = await GetExistingTaskIdsAsync(userId, from, to, cancellationToken);
        var suggestions = Engine.Build(signals, from, to);
        var now = DateTime.UtcNow;
        var toInsert = new List<SuggestionModel>();

        foreach (var suggestion in suggestions)
        {
            if ((suggestion.TaskId is not null && existingTaskIds.Contains(suggestion.TaskId)) || await IsAlreadyGeneratedAsync(userId, suggestion))
            {
                continue;
            }

            toInsert.Add(suggestion.ToModel(userId, now));
        }

        await Repository.InsertManyAsync(toInsert);
        return toInsert.Count;
    }

    private async Task<HashSet<string>> GetExistingTaskIdsAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var entries = await TimeEntryService.ListAsync(userId, from, to, cancellationToken);
        if (entries.IsError)
        {
            Logger.LogWarning("Could not load time entries for suggestion deduplication for user {UserId}: {Errors}", userId, entries.Errors);
            return [];
        }

        return entries.Value
            .Select(x => x.TaskId)
            .OfType<string>()
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    // TODO could be solved with one mongo query
    private async Task<bool> IsAlreadyGeneratedAsync(string userId, TimeSuggestion suggestion)
    {
        foreach (var source in suggestion.Sources)
        {
            if (await Repository.ExistsBySourceAsync(userId, source.ConnectorKey, source.ExternalId))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<(ConnectorOutcomeContract Outcome, IReadOnlyList<ActivitySignal>? Signals)> FetchFromConnectorAsync(ConnectorDescriptor descriptor, string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        try
        {
            var resolved = await Resolver.ResolveConnectedAsync(descriptor.Key, userId, cancellationToken);
            if (resolved.IsError)
            {
                var resolveStatus = StatusFor(resolved.FirstError);
                return (new ConnectorOutcomeContract(descriptor.Key, resolveStatus, 0, resolved.FirstError.Description), null);
            }

            var container = new ActivityFetchContainer { From = from, To = to };
            var fetch = await resolved.Value.FetchAsync(container, cancellationToken);
            if (fetch.IsError)
            {
                var fetchStatus = StatusFor(fetch.FirstError);
                return (new ConnectorOutcomeContract(descriptor.Key, fetchStatus, 0, fetch.FirstError.Description), null);
            }

            var fetchedSignals = fetch.Value;
            return (new ConnectorOutcomeContract(descriptor.Key, "Success", fetchedSignals.Count, null), fetchedSignals);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "Connector {ConnectorKey} threw while fetching suggestion signals for user {UserId}", descriptor.Key, userId);
            return (new ConnectorOutcomeContract(descriptor.Key, "Error", 0, ex.Message), null);
        }
    }

    private static string StatusFor(Error error)
    {
        return error.Type switch
        {
            ErrorType.Conflict => "NotConnected",
            ErrorType.Unauthorized => "AuthFailed",
            _ => "Error",
        };
    }
}

public interface ISuggestionService
{
    Task<GenerateSuggestionsResultContract> GenerateAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken);

    Task<GenerateSuggestionsResultContract> ReloadAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken);

    Task<IReadOnlyList<SuggestionContract>> ListAsync(string userId, DateTime from, DateTime to);

    Task<ErrorOr<SuggestionContract>> UpdateAsync(string userId, string id, SuggestionUpdateContract request, CancellationToken cancellationToken);

    Task<ErrorOr<Deleted>> DismissAsync(string userId, string id, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> AcceptAsync(string userId, string id, CancellationToken cancellationToken);
}
