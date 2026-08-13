using ErrorOr;

namespace Vuetrack.Api.Features.Integrations;

public static class IntegrationError
{
    public static readonly Error NotConnected = Error.Conflict(code: "Integration.NotConnected", description: "The integration is not connected");
}
