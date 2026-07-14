namespace Vuetrack.Api.Features.Suggestions.Core.Contracts;

public sealed record SuggestionUpdateContract
{
    public required string Title { get; init; }

    public string? TaskId { get; init; }

    public string? ProjectId { get; init; }

    public string? ActivityId { get; init; }

    public required DateTime DateStarted { get; init; }

    public required DateTime DateEnded { get; init; }

    public string? Comment { get; init; }
}
