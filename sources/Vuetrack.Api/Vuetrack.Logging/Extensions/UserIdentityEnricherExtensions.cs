using Serilog;
using Serilog.Configuration;
using Vuetrack.Logging.Enricher;

namespace Vuetrack.Logging.Extensions;

public static class UserIdentityEnricherExtensions
{
    public static void WithUserIdentity(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        ArgumentNullException.ThrowIfNull(enrichmentConfiguration);

        enrichmentConfiguration.With(new UserIdentityEnricher());
    }
}
