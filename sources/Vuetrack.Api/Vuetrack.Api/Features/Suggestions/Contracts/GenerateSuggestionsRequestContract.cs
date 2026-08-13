namespace Vuetrack.Api.Features.Suggestions.Contracts;

public sealed record GenerateSuggestionsRequestContract
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }
}
