using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Suggestions.Engine;

public interface ISuggestionEngine
{
    IReadOnlyList<TimeSuggestion> Build(IReadOnlyList<ActivitySignal> signals, DateTimeOffset from, DateTimeOffset to);
}
