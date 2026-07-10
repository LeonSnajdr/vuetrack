namespace Vuetrack.Api.Features.Suggestions;

public sealed class SuggestionSourceModel
{
    public required string ConnectorKey { get; set; }

    public required string ExternalId { get; set; }

    public string? Link { get; set; }
}
