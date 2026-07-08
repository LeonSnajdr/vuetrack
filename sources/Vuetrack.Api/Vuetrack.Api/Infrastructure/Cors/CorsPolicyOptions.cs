using Samhammer.Options.Abstractions;

namespace Vuetrack.Api.Infrastructure.Cors;

[Option]
public class CorsPolicyOptions
{
    public required List<string> DomainUrls { get; init; } = [];
}
