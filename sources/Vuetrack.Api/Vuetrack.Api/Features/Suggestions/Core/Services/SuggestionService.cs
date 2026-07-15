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

    public async Task<IReadOnlyList<SuggestionContract>> GenerateAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken)
    {
        var descriptors = SelectDescriptors(request);
        var signals = new List<ActivitySignal>();

        foreach (var descriptor in descriptors)
        {
            var fetched = await FetchFromConnectorAsync(descriptor, userId, request.From, request.To, cancellationToken);
            if (fetched is not null)
            {
                signals.AddRange(fetched);
            }
        }

        var inserted = await BuildAndInsertAsync(userId, request.From, request.To, signals, cancellationToken);
        return inserted;
    }

    public async Task<IReadOnlyList<SuggestionContract>> ReloadAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken)
    {
        var descriptors = SelectDescriptors(request);
        var signals = new List<ActivitySignal>();
        var successfulConnectorKeys = new List<ConnectorKey>();

        foreach (var descriptor in descriptors)
        {
            var fetched = await FetchFromConnectorAsync(descriptor, userId, request.From, request.To, cancellationToken);
            if (fetched is not null)
            {
                successfulConnectorKeys.Add(descriptor.Key);
                signals.AddRange(fetched);
            }
        }

        if (successfulConnectorKeys.Count > 0)
        {
            await Repository.DeleteResettableAsync(userId, request.From, request.To, successfulConnectorKeys);
        }

        var inserted = await BuildAndInsertAsync(userId, request.From, request.To, signals, cancellationToken);
        return inserted;
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

    private async Task<IReadOnlyList<SuggestionContract>> BuildAndInsertAsync(string userId, DateTime from, DateTime to, IReadOnlyList<ActivitySignal> signals, CancellationToken cancellationToken)
    {
        var existingTaskIds = await GetExistingTaskIdsAsync(userId, from, to, cancellationToken);
        var built = await Engine.BuildAsync(signals, from, to, cancellationToken);
        if (built.IsError)
        {
            Logger.LogWarning("Suggestion engine failed to build suggestions for user {UserId}: {Errors}", userId, built.Errors);
            return [];
        }

        var suggestions = built.Value;
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

        var contracts = toInsert.Select(m => m.ToContract()).ToList();
        return contracts;
    }

    private async Task<HashSet<string>> GetExistingTaskIdsAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var entries = await TimeEntryService.ListAsync(userId, from, to, cancellationToken);
        if (entries.IsError)
        {
            // TODO should also return ErrorOr and fail at this point already
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
    private async Task<bool> IsAlreadyGeneratedAsync(string userId, SuggestionEngineResult result)
    {
        foreach (var source in result.Sources)
        {
            if (await Repository.ExistsBySourceAsync(userId, source.ConnectorKey, source.ExternalId))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<IReadOnlyList<ActivitySignal>?> FetchFromConnectorAsync(ConnectorDescriptor descriptor, string userId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        try
        {
            var resolved = await Resolver.ResolveConnectedAsync(descriptor.Key, userId, cancellationToken);
            if (resolved.IsError)
            {
                Logger.LogInformation("Connector {ConnectorKey} unavailable for user {UserId}: {Error}", descriptor.Key, userId, resolved.FirstError.Description);
                return null;
            }

            var container = new ActivityFetchContainer { From = from, To = to };
            var fetch = await resolved.Value.FetchAsync(container, cancellationToken);
            if (fetch.IsError)
            {
                Logger.LogWarning("Connector {ConnectorKey} failed to fetch signals for user {UserId}: {Error}", descriptor.Key, userId, fetch.FirstError.Description);
                return null;
            }

            return fetch.Value;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "Connector {ConnectorKey} threw while fetching suggestion signals for user {UserId}", descriptor.Key, userId);
            return null;
        }
    }
}

public interface ISuggestionService
{
    Task<IReadOnlyList<SuggestionContract>> GenerateAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken);

    Task<IReadOnlyList<SuggestionContract>> ReloadAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken);

    Task<IReadOnlyList<SuggestionContract>> ListAsync(string userId, DateTime from, DateTime to);

    Task<ErrorOr<SuggestionContract>> UpdateAsync(string userId, string id, SuggestionUpdateContract request, CancellationToken cancellationToken);

    Task<ErrorOr<Deleted>> DismissAsync(string userId, string id, CancellationToken cancellationToken);

    Task<ErrorOr<Success>> AcceptAsync(string userId, string id, CancellationToken cancellationToken);
}
