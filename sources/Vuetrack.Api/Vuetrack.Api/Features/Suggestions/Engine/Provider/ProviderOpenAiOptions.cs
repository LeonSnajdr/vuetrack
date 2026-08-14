using Samhammer.Options.Abstractions;

namespace Vuetrack.Api.Features.Suggestions.Engine.Provider;

[Option]
public class ProviderOpenAiOptions
{
    public required string ApiKey { get; init; }

    public required string Endpoint { get; init; }

    public required string Model { get; init; }

    public required int MaxOutputTokens { get; init; }
}
