using Serilog;
using Serilog.Configuration;

namespace Vuetrack.Api.Infrastructure.Logging;

public static class UserIdentityEnricherExtensions
{
    public static void WithUserIdentity(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        ArgumentNullException.ThrowIfNull(enrichmentConfiguration);

        enrichmentConfiguration.With(new UserIdentityEnricher());
    }
}
