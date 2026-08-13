namespace Vuetrack.Api.Features.Integrations;

public sealed record ActivityFetchContainer
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }
}
