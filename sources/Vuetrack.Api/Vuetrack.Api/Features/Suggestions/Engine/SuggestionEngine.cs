using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Suggestions.Engine.Provider;

namespace Vuetrack.Api.Features.Suggestions.Engine;

[Inject]
public sealed class SuggestionEngine(ISuggestionProvider provider, ILogger<SuggestionEngine> logger) : ISuggestionEngine
{
    private ISuggestionProvider Provider { get; } = provider;

    private ILogger<SuggestionEngine> Logger { get; } = logger;

    public async Task<ErrorOr<IReadOnlyList<SuggestionEngineResult>>> BuildAsync(IReadOnlyList<ActivitySignal> signals, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var deduplicated = Deduplicate(signals);
        if (deduplicated.Count == 0)
        {
            IReadOnlyList<SuggestionEngineResult> empty = [];
            return empty.ToErrorOr();
        }

        var context = BuildContext(deduplicated, from, to);
        var candidates = await Provider.ProvideAsync(context, cancellationToken);
        if (candidates.IsError)
        {
            return candidates.Errors;
        }

        var byExternalId = IndexByExternalId(deduplicated);
        var suggestions = new List<SuggestionEngineResult>();
        var discarded = 0;
        foreach (var candidate in candidates.Value)
        {
            var mapped = MapCandidate(candidate, byExternalId, from, to);
            if (mapped is null)
            {
                discarded++;
                continue;
            }

            suggestions.Add(mapped);
        }

        if (discarded > 0)
        {
            Logger.LogInformation("Discarded {DiscardedCount} suggestion candidates with unknown sources or an empty time range", discarded);
        }

        var ordered = suggestions
            .OrderBy(s => s.DateStarted)
            .ThenBy(s => s.Sources.Count > 0 ? s.Sources[0].ExternalId : string.Empty, StringComparer.Ordinal)
            .ToList();

        return ordered;
    }

    private static List<ActivitySignal> Deduplicate(IReadOnlyList<ActivitySignal> signals)
    {
        var byKey = new Dictionary<(IntegrationKey Key, string ExternalId), ActivitySignal>();

        foreach (var signal in signals)
        {
            var key = (signal.Key, signal.ExternalId);
            if (!byKey.TryGetValue(key, out var existing))
            {
                byKey[key] = signal;
                continue;
            }

            var start = existing.DateStarted < signal.DateStarted ? existing.DateStarted : signal.DateStarted;
            var end = MaxEnd(existing.DateEnded, signal.DateEnded);
            byKey[key] = existing with { DateStarted = start, DateEnded = end };
        }

        return byKey.Values.ToList();
    }

    private static DateTime? MaxEnd(DateTime? left, DateTime? right)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        return left > right ? left : right;
    }

    private static Dictionary<string, ActivitySignal> IndexByExternalId(List<ActivitySignal> signals)
    {
        var byExternalId = new Dictionary<string, ActivitySignal>(StringComparer.Ordinal);
        foreach (var signal in signals)
        {
            byExternalId[signal.ExternalId] = signal;
        }

        return byExternalId;
    }

    private static SuggestionProviderContext BuildContext(List<ActivitySignal> signals, DateTime from, DateTime to)
    {
        return new SuggestionProviderContext
        {
            From = from,
            To = to,
            Signals = signals,
        };
    }

    private static SuggestionEngineResult? MapCandidate(SuggestionProviderCandidate candidate, Dictionary<string, ActivitySignal> byExternalId, DateTime from, DateTime to)
    {
        var sources = ResolveSources(candidate.SourceExternalIds, byExternalId);
        if (sources.Count == 0)
        {
            return null;
        }

        var start = candidate.DateStarted < from ? from : candidate.DateStarted;
        var end = candidate.DateEnded > to ? to : candidate.DateEnded;
        if (end <= start)
        {
            return null;
        }

        var confidence = Math.Min(1.0, candidate.Confidence);
        var evidence = BuildEvidence(sources, confidence);

        return new SuggestionEngineResult
        {
            TaskId = candidate.TaskId,
            Comment = candidate.Comment,
            DateStarted = start,
            DateEnded = end,
            Confidence = confidence,
            Sources = evidence,
        };
    }

    private static List<ActivitySignal> ResolveSources(IReadOnlyList<string> externalIds, Dictionary<string, ActivitySignal> byExternalId)
    {
        var resolved = new List<ActivitySignal>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var externalId in externalIds)
        {
            if (!seen.Add(externalId))
            {
                continue;
            }

            if (byExternalId.TryGetValue(externalId, out var signal))
            {
                resolved.Add(signal);
            }
        }

        var ordered = resolved
            .OrderBy(s => s.DateStarted)
            .ThenBy(s => s.ExternalId, StringComparer.Ordinal)
            .ToList();

        return ordered;
    }

    private static List<SuggestionEngineEvidence> BuildEvidence(List<ActivitySignal> sources, double confidence)
    {
        var evidence = new List<SuggestionEngineEvidence>(sources.Count);
        foreach (var signal in sources)
        {
            var end = signal.DateEnded ?? signal.DateStarted;
            var item = new SuggestionEngineEvidence
            {
                Key = signal.Key,
                ExternalId = signal.ExternalId,
                Kind = signal.Kind,
                DateStarted = signal.DateStarted,
                DateEnded = end,
                Confidence = confidence,
            };

            evidence.Add(item);
        }

        return evidence;
    }
}

public interface ISuggestionEngine
{
    Task<ErrorOr<IReadOnlyList<SuggestionEngineResult>>> BuildAsync(IReadOnlyList<ActivitySignal> signals, DateTime from, DateTime to, CancellationToken cancellationToken);
}
