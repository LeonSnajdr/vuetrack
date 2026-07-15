using Samhammer.Options.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Engine;

[Option]
public sealed record SuggestionEngineOptions
{
    // Interval shaping.
    public TimeSpan MergeGap { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan RoundTo { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan MinimumBlock { get; init; } = TimeSpan.FromMinutes(5);

    // Bounded windows applied to point events by the evidence rules.
    public TimeSpan CommitWindow { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan CorroborationWindow { get; init; } = TimeSpan.FromMinutes(10);

    // Evidence weights.
    public double ExplicitWeight { get; init; } = 1.0;

    public double CommitWeight { get; init; } = 0.5;

    public double CorroborationWeight { get; init; } = 0.3;

    public double ContextWeight { get; init; } = 0.0;

    // A block promotes to a suggestion when it has an explicit-duration contributor or its combined
    // weight reaches this threshold.
    public double ConfidenceThreshold { get; init; } = 0.6;
}
