namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionUpdateContract
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required DateTimeOffset Start { get; init; }

    public required DateTimeOffset End { get; init; }
}
