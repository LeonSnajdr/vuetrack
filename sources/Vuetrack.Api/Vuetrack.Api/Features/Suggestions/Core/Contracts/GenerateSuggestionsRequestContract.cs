using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record GenerateSuggestionsRequestContract
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }

    public IReadOnlyList<ConnectorKey>? ConnectorKeys { get; init; }
}
