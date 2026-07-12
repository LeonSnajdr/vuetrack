using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Suggestions.Core.Contracts;
using Vuetrack.Api.Features.Suggestions.Engine;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core.Services;

[Inject]
public class SuggestionService(IConnectorRegistry registry, IEnumerable<IConnectorContextInitializer> contextInitializers, ISuggestionRepository repository, ISuggestionEngine engine, ILogger<SuggestionService> logger) : ISuggestionService
{
    private IConnectorRegistry Registry { get; } = registry;

    private IReadOnlyList<IConnectorContextInitializer> ContextInitializers { get; } = contextInitializers.ToList();

    private ISuggestionRepository Repository { get; } = repository;

    private ISuggestionEngine Engine { get; } = engine;

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

        var suggestions = Engine.Build(signals, request.From, request.To);
        var now = DateTimeOffset.UtcNow;
        var toInsert = new List<SuggestionModel>();

        foreach (var suggestion in suggestions)
        {
            if (await IsAlreadyGeneratedAsync(userId, suggestion))
            {
                continue;
            }

            toInsert.Add(suggestion.ToModel(userId, now));
        }

        if (toInsert.Count > 0)
        {
            await Repository.InsertManyAsync(toInsert);
        }

        return new GenerateSuggestionsResultContract(toInsert.Count, outcomes);
    }

    public async Task<IReadOnlyList<SuggestionContract>> ListAsync(string userId, DateTimeOffset from, DateTimeOffset to)
    {
        var models = await Repository.ListAsync(userId, from, to);
        return models.Select(m => m.ToContract()).ToList();
    }

    public async Task<SuggestionUpdateResult> UpdateAsync(string userId, string id, SuggestionUpdateContract request, CancellationToken cancellationToken)
    {
        var updated = await Repository.UpdateFieldsAsync(id, userId, request.Title, request.Description, request.Start, request.End, DateTimeOffset.UtcNow);

        return updated is null ? new SuggestionNotFound() : new SuggestionUpdated(updated.ToContract());
    }

    public async Task<SuggestionDismissResult> DismissAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var found = await Repository.SetStatusAsync(id, userId, SuggestionStatus.Dismissed, DateTimeOffset.UtcNow);

        return found ? new SuggestionDismissed() : new SuggestionDismissNotFound();
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

    private async Task<(ConnectorOutcomeContract Outcome, IReadOnlyList<ActivitySignal>? Signals)> FetchFromConnectorAsync(ConnectorDescriptor descriptor, string userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        try
        {
            var initializer = ContextInitializers.FirstOrDefault(i => i.ConnectorKey == descriptor.Key);
            if (initializer is null)
            {
                return (new ConnectorOutcomeContract(descriptor.Key, "Error", 0, "No context initializer registered for this connector."), null);
            }

            var initialized = await initializer.TryInitializeAsync(userId, cancellationToken);
            if (!initialized)
            {
                return (new ConnectorOutcomeContract(descriptor.Key, "NotConnected", 0, null), null);
            }

            var connector = Registry.Resolve(descriptor.Key);
            if (connector is null)
            {
                return (new ConnectorOutcomeContract(descriptor.Key, "Error", 0, "Connector is not registered."), null);
            }

            var result = await connector.FetchAsync(new ActivityFetchContainer { From = from, To = to }, cancellationToken);

            return result switch
            {
                ActivityFetchSuccess success => (new ConnectorOutcomeContract(descriptor.Key, "Success", success.Signals.Count, null), success.Signals),
                ActivityFetchNotConnected => (new ConnectorOutcomeContract(descriptor.Key, "NotConnected", 0, null), null),
                ActivityFetchAuthFailed authFailed => (new ConnectorOutcomeContract(descriptor.Key, "AuthFailed", 0, authFailed.Reason), null),
                ActivityFetchRateLimited rateLimited => (new ConnectorOutcomeContract(descriptor.Key, "RateLimited", 0, $"Retry after {rateLimited.RetryAfter.TotalSeconds:0}s"), null),
                ActivityFetchConnectorError error => (new ConnectorOutcomeContract(descriptor.Key, "Error", 0, error.Message), null),
                _ => throw new InvalidOperationException($"Unhandled {nameof(ActivityFetchResult)} case."),
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "Connector {ConnectorKey} threw while fetching suggestion signals for user {UserId}", descriptor.Key, userId);
            return (new ConnectorOutcomeContract(descriptor.Key, "Error", 0, ex.Message), null);
        }
    }
}

public interface ISuggestionService
{
    Task<GenerateSuggestionsResultContract> GenerateAsync(string userId, GenerateSuggestionsRequestContract request, CancellationToken cancellationToken);

    Task<IReadOnlyList<SuggestionContract>> ListAsync(string userId, DateTimeOffset from, DateTimeOffset to);

    Task<SuggestionUpdateResult> UpdateAsync(string userId, string id, SuggestionUpdateContract request, CancellationToken cancellationToken);

    Task<SuggestionDismissResult> DismissAsync(string userId, string id, CancellationToken cancellationToken);
}
