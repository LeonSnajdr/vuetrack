namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record GenerateSuggestionsRequestContract
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }

    public IReadOnlyList<string>? ConnectorKeys { get; init; }
}
