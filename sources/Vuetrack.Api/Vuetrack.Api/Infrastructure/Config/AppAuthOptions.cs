using Samhammer.Options.Abstractions;

namespace Vuetrack.Api.Infrastructure.Config;

[Option]
public class AppAuthOptions
{
    public required string AuthUrl { get; init; }

    public required string Realm { get; init; }

    public required string AppClientId { get; init; }

    public required string ApiClientId { get; init; }
}
