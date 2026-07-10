namespace Vuetrack.Api.Features.Suggestions.Contracts;

public sealed record GenerateSuggestionsRequestContract
{
    public required DateTimeOffset From { get; init; }

    public required DateTimeOffset To { get; init; }

    public IReadOnlyList<string>? ConnectorKeys { get; init; }
}
