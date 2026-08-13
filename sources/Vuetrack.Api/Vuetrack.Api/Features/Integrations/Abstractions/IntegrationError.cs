using ErrorOr;
using Vuetrack.Api.Features.Integrations.Abstractions;

namespace Vuetrack.Api.Features.Integrations.Abstractions;

public static class IntegrationError
{
    public static readonly Error NotConnected = Error.Conflict(code: "Integration.NotConnected", description: "The integration is not connected");
}
