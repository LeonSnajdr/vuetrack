namespace Vuetrack.Api.Features.Details.Contracts;

public sealed record DetailQuery
{
    public string? TaskId { get; init; }
}
