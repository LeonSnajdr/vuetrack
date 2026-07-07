using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Vuetrack.Logging.Enricher;

public class UserIdentityEnricher : ILogEventEnricher
{
    private IHttpContextAccessor ContextAccessor { get; }

    public UserIdentityEnricher()
        : this(new HttpContextAccessor())
    {
    }

    private UserIdentityEnricher(IHttpContextAccessor contextAccessor)
    {
        ContextAccessor = contextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = ContextAccessor.HttpContext;

        var identity = httpContext?.User.Identity;
        if (identity is not { IsAuthenticated: true })
        {
            return;
        }

        var userIdentityNameProperty = new LogEventProperty("UserIdentityName", new ScalarValue(identity.Name));

        logEvent.AddPropertyIfAbsent(userIdentityNameProperty);
    }
}
