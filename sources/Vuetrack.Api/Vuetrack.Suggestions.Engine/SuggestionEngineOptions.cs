namespace Vuetrack.Suggestions.Engine;

public sealed record SuggestionEngineOptions
{
    public TimeSpan MergeGap { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan RoundTo { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan MinimumBlock { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan DefaultPointDuration { get; init; } = TimeSpan.FromMinutes(15);

    public string CorrelationMetadataKey { get; init; } = "correlationId";
}
