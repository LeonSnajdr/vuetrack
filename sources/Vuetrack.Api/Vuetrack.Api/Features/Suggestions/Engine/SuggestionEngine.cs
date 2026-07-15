using System.Text.Json;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Suggestions.Engine.Rules;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Engine;

[Inject]
public sealed class SuggestionEngine(IOptions<SuggestionEngineOptions> options) : ISuggestionEngine
{
    private static readonly IReadOnlyList<IEvidenceRule> Rules =
    [
        new ExplicitDurationRule(),
        new CommitRule(),
        new StatusTransitionRule(),
        new CorroboratingRule(),
        new ContextRule(),
    ];

    private SuggestionEngineOptions Options { get; } = options.Value;

    public IReadOnlyList<TimeSuggestion> Build(IReadOnlyList<ActivitySignal> signals, DateTime from, DateTime to)
    {
        var normalized = Normalize(signals);
        var deduplicated = Deduplicate(normalized);
        var candidates = ToCandidates(deduplicated, from, to);
        var blocks = GroupIntoBlocks(candidates);

        var suggestions = new List<TimeSuggestion>();
        foreach (var block in blocks)
        {
            var suggestion = TryBuildSuggestion(block);
            if (suggestion is not null)
            {
                suggestions.Add(suggestion);
            }
        }

        var ordered = suggestions
            .OrderBy(s => s.DateStarted)
            .ThenBy(s => s.Sources.Count > 0 ? s.Sources[0].ExternalId : string.Empty, StringComparer.Ordinal)
            .ToList();

        return ordered;
    }

    private static List<NormalizedSignal> Normalize(IReadOnlyList<ActivitySignal> signals)
    {
        var result = new List<NormalizedSignal>(signals.Count);
        foreach (var signal in signals)
        {
            var normalized = SignalNormalizer.Normalize(signal);
            result.Add(normalized);
        }

        return result;
    }

    private static List<NormalizedSignal> Deduplicate(List<NormalizedSignal> signals)
    {
        var byKey = new Dictionary<(ConnectorKey ConnectorKey, string ExternalId), NormalizedSignal>();

        foreach (var signal in signals)
        {
            var key = (signal.ConnectorKey, signal.ExternalId);
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

    private List<CandidateEvidence> ToCandidates(List<NormalizedSignal> signals, DateTime from, DateTime to)
    {
        var candidates = new List<CandidateEvidence>();

        foreach (var signal in signals)
        {
            var rule = FindRule(signal);
            if (rule is null)
            {
                continue;
            }

            var candidate = rule.Create(signal, Options);
            var clamped = Clamp(candidate, from, to);
            if (clamped is not null)
            {
                candidates.Add(clamped);
            }
        }

        return candidates;
    }

    private static IEvidenceRule? FindRule(NormalizedSignal signal)
    {
        foreach (var rule in Rules)
        {
            if (rule.AppliesTo(signal))
            {
                return rule;
            }
        }

        return null;
    }

    private static CandidateEvidence? Clamp(CandidateEvidence candidate, DateTime from, DateTime to)
    {
        var start = candidate.DateStarted < from ? from : candidate.DateStarted;
        var end = candidate.DateEnded > to ? to : candidate.DateEnded;
        if (end <= start)
        {
            return null;
        }

        return candidate with { DateStarted = start, DateEnded = end };
    }

    private List<Block> GroupIntoBlocks(List<CandidateEvidence> candidates)
    {
        var blocks = new List<Block>();

        var groups = candidates
            .GroupBy(c => c.Signal.PartitionKey, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var ordered = group
                .OrderBy(c => c.DateStarted)
                .ThenBy(c => c.Signal.ExternalId, StringComparer.Ordinal)
                .ToList();

            List<CandidateEvidence>? current = null;
            var blockEnd = default(DateTime);

            foreach (var candidate in ordered)
            {
                if (current is null)
                {
                    current = [candidate];
                    blockEnd = candidate.DateEnded;
                    continue;
                }

                var gap = candidate.DateStarted - blockEnd;
                if (gap <= Options.MergeGap)
                {
                    current.Add(candidate);
                    if (candidate.DateEnded > blockEnd)
                    {
                        blockEnd = candidate.DateEnded;
                    }

                    continue;
                }

                blocks.Add(new Block(current));
                current = [candidate];
                blockEnd = candidate.DateEnded;
            }

            if (current is not null)
            {
                blocks.Add(new Block(current));
            }
        }

        return blocks;
    }

    private TimeSuggestion? TryBuildSuggestion(Block block)
    {
        var contributors = block.Candidates;
        var rawStart = contributors.Min(c => c.DateStarted);
        var rawEnd = contributors.Max(c => c.DateEnded);
        var start = RoundDown(rawStart, Options.RoundTo);
        var end = RoundUp(rawEnd, Options.RoundTo);

        if (end - start < Options.MinimumBlock)
        {
            return null;
        }

        var totalWeight = contributors.Sum(c => c.Weight);
        var hasExplicit = contributors.Any(c => c.HasExplicitDuration);
        var canCreateAlone = contributors.Any(c => c.CanCreateAlone);
        var canPromote = hasExplicit || canCreateAlone || totalWeight >= Options.ConfidenceThreshold;
        if (!canPromote)
        {
            return null;
        }

        var ordered = contributors
            .OrderByDescending(c => c.Weight)
            .ThenBy(c => c.DateStarted)
            .ThenBy(c => c.Signal.ExternalId, StringComparer.Ordinal)
            .ToList();

        var taskId = ordered.Select(c => c.Signal.SubjectWorkItemId).FirstOrDefault(v => !string.IsNullOrEmpty(v));
        var projectName = ordered.Select(c => c.Signal.DisplayProject).FirstOrDefault(v => !string.IsNullOrEmpty(v));
        var comment = ordered.Select(c => c.Signal.DisplayComment).FirstOrDefault(v => !string.IsNullOrEmpty(v));
        var confidence = Math.Min(1.0, totalWeight);
        var canonical = BuildCanonicalMetadata(ordered);
        var sources = ordered.Select(ToEvidence).ToList();

        return new TimeSuggestion(taskId, projectName, comment, start, end, confidence, canonical, sources);
    }

    private static IReadOnlyDictionary<string, JsonElement> BuildCanonicalMetadata(IReadOnlyList<CandidateEvidence> orderedByStrength)
    {
        var canonical = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var candidate in orderedByStrength)
        {
            foreach (var pair in candidate.Signal.Metadata)
            {
                if (!canonical.ContainsKey(pair.Key))
                {
                    canonical[pair.Key] = pair.Value;
                }
            }
        }

        return canonical;
    }

    private static SuggestionEvidence ToEvidence(CandidateEvidence candidate)
    {
        var signal = candidate.Signal;
        return new SuggestionEvidence(
            signal.ConnectorKey,
            signal.ExternalId,
            signal.Kind,
            candidate.DateStarted,
            candidate.DateEnded,
            candidate.Weight,
            signal.Metadata);
    }

    private static DateTime RoundDown(DateTime value, TimeSpan step)
    {
        if (step <= TimeSpan.Zero)
        {
            return value;
        }

        var ticks = value.Ticks - (value.Ticks % step.Ticks);
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    private static DateTime RoundUp(DateTime value, TimeSpan step)
    {
        if (step <= TimeSpan.Zero)
        {
            return value;
        }

        var remainder = value.Ticks % step.Ticks;
        if (remainder == 0)
        {
            return value;
        }

        var ticks = value.Ticks - remainder + step.Ticks;
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    private sealed record Block(IReadOnlyList<CandidateEvidence> Candidates);
}

public interface ISuggestionEngine
{
    IReadOnlyList<TimeSuggestion> Build(IReadOnlyList<ActivitySignal> signals, DateTime from, DateTime to);
}
