using Serilog;
using Serilog.Configuration;
using Vuetrack.Api.Infrastructure.Logging.Enricher;

namespace Vuetrack.Api.Infrastructure.Logging.Extensions;

public static class UserIdentityEnricherExtensions
{
    public static void WithUserIdentity(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        ArgumentNullException.ThrowIfNull(enrichmentConfiguration);

        enrichmentConfiguration.With(new UserIdentityEnricher());
    }
}
