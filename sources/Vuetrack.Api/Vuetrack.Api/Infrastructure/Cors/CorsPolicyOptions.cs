using Samhammer.Options.Abstractions;

namespace Vuetrack.Api.Initialization;

[Option]
public class CorsPolicyOptions
{
    public required List<string> DomainUrls { get; init; }
}
