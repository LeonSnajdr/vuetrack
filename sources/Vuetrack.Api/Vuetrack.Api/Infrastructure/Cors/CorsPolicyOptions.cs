using Samhammer.Options.Abstractions;

namespace Vuetrack.Api.Infrastructure.Cors;

[Option]
public class CorsPolicyOptions
{
    public List<string>? DomainUrls { get; init; }
}
