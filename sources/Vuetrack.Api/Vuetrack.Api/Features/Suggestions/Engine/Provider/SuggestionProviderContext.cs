using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Engine.Provider;

public sealed record SuggestionProviderContext
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }

    public required IReadOnlyList<ActivitySignal> Signals { get; init; }
}
