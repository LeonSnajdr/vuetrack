using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Engine;

[Inject]
public sealed class SuggestionEngine(IOptions<SuggestionEngineOptions> options) : ISuggestionEngine
{
    private SuggestionEngineOptions Options { get; } = options.Value;

    public IReadOnlyList<TimeSuggestion> Build(IReadOnlyList<ActivitySignal> signals, DateTime from, DateTime to)
    {
        var normalized = Normalize(signals, from, to);
        var deduplicated = Deduplicate(normalized);
        var blocks = GroupIntoBlocks(deduplicated);

        var suggestions = new List<TimeSuggestion>();

        foreach (var block in blocks)
        {
            var start = RoundDown(block.Start, Options.RoundTo);
            var end = RoundUp(block.End, Options.RoundTo);

            if (end - start < Options.MinimumBlock)
            {
                continue;
            }

            suggestions.Add(BuildSuggestion(block.Signals, start, end));
        }

        return suggestions
            .OrderBy(s => s.Start)
            .ThenBy(s => s.Title, StringComparer.Ordinal)
            .ToList();
    }

    private IReadOnlyList<NormalizedSignal> Normalize(IReadOnlyList<ActivitySignal> signals, DateTime from, DateTime to)
    {
        var result = new List<NormalizedSignal>();

        foreach (var signal in signals)
        {
            var hasExplicitDuration = signal.End.HasValue;
            var effectiveEnd = signal.End ?? signal.Start + Options.DefaultPointDuration;

            if (effectiveEnd <= from || signal.Start >= to)
            {
                continue;
            }

            var start = signal.Start < from ? from : signal.Start;
            var end = effectiveEnd > to ? to : effectiveEnd;

            if (end <= start)
            {
                continue;
            }

            var correlationKey = signal.Metadata.TryGetValue(Options.CorrelationMetadataKey, out var explicitKey) && !string.IsNullOrEmpty(explicitKey)
                ? explicitKey
                : $"{signal.ConnectorKey.ToString()}|{signal.Title}";

            result.Add(new NormalizedSignal(signal.ConnectorKey, signal.ExternalId, signal.Title, signal.Description, start, end, signal.Link, hasExplicitDuration, correlationKey));
        }

        return result;
    }

    private static IReadOnlyList<NormalizedSignal> Deduplicate(IReadOnlyList<NormalizedSignal> signals)
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

            byKey[key] = existing with
            {
                Start = existing.Start < signal.Start ? existing.Start : signal.Start,
                End = existing.End > signal.End ? existing.End : signal.End,
                HasExplicitDuration = existing.HasExplicitDuration || signal.HasExplicitDuration,
                Description = existing.Description ?? signal.Description,
                Link = existing.Link ?? signal.Link,
            };
        }

        return byKey.Values.ToList();
    }

    private IReadOnlyList<SignalBlock> GroupIntoBlocks(IReadOnlyList<NormalizedSignal> signals)
    {
        var blocks = new List<SignalBlock>();

        var groups = signals
            .GroupBy(s => s.CorrelationKey, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(s => s.Start).ThenBy(s => s.ExternalId, StringComparer.Ordinal).ToList();

            List<NormalizedSignal>? current = null;
            var blockStart = default(DateTime);
            var blockEnd = default(DateTime);

            foreach (var signal in ordered)
            {
                if (current is null)
                {
                    current = [signal];
                    blockStart = signal.Start;
                    blockEnd = signal.End;
                    continue;
                }

                var gap = signal.Start - blockEnd;
                if (gap <= Options.MergeGap)
                {
                    current.Add(signal);
                    if (signal.End > blockEnd)
                    {
                        blockEnd = signal.End;
                    }

                    continue;
                }

                blocks.Add(new SignalBlock(blockStart, blockEnd, current));
                current = [signal];
                blockStart = signal.Start;
                blockEnd = signal.End;
            }

            if (current is not null)
            {
                blocks.Add(new SignalBlock(blockStart, blockEnd, current));
            }
        }

        return blocks;
    }

    private static TimeSuggestion BuildSuggestion(IReadOnlyList<NormalizedSignal> contributors, DateTime start, DateTime end)
    {
        var ordered = contributors
            .OrderBy(s => s.Start)
            .ThenBy(s => s.ConnectorKey)
            .ThenBy(s => s.ExternalId, StringComparer.Ordinal)
            .ToList();

        var title = ordered[0].Title;
        var description = ordered.Select(s => s.Description).FirstOrDefault(d => !string.IsNullOrEmpty(d));

        // Signals with an explicit duration (e.g. worklogs) are stronger evidence than inferred point
        // events; each additional corroborating source raises confidence, capped at 1.0.
        var baseConfidence = ordered.Any(s => s.HasExplicitDuration) ? 0.6 : 0.3;
        var confidence = Math.Min(1.0, baseConfidence + (0.15 * (ordered.Count - 1)));

        var sources = ordered
            .Select(s => new SignalRef(s.ConnectorKey, s.ExternalId, s.Link))
            .ToList();

        return new TimeSuggestion(title, description, start, end, confidence, sources);
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

    private sealed record NormalizedSignal(
        ConnectorKey ConnectorKey,
        string ExternalId,
        string Title,
        string? Description,
        DateTime Start,
        DateTime End,
        string? Link,
        bool HasExplicitDuration,
        string CorrelationKey);

    private sealed record SignalBlock(DateTime Start, DateTime End, IReadOnlyList<NormalizedSignal> Signals);
}

public interface ISuggestionEngine
{
    IReadOnlyList<TimeSuggestion> Build(IReadOnlyList<ActivitySignal> signals, DateTime from, DateTime to);
}
