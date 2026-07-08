using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace Vuetrack.Api.Infrastructure.Cors;

public class ConfigureCors(IOptions<CorsPolicyOptions> options) : IConfigureOptions<CorsOptions>
{
    private IOptions<CorsPolicyOptions> Options { get; } = options;

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
