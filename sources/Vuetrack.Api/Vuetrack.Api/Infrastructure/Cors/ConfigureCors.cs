using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace Vuetrack.Api.Initialization;

public class ConfigureCors : IConfigureOptions<CorsOptions>
{
    private IOptions<CorsPolicyOptions> Options { get; }

    public ConfigureCors(IOptions<CorsPolicyOptions> options)
    {
        Options = options;
    }

    public void Configure(CorsOptions options)
    {
        options.AddDefaultPolicy(builder =>
        {
            builder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithOrigins(Options.Value.DomainUrls.ToArray())
                .SetIsOriginAllowedToAllowWildcardSubdomains();
        });
    }
}
