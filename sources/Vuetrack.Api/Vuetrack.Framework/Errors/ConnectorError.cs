using ErrorOr;

namespace Vuetrack.Framework.Errors;

public static class ConnectorError
{
    public static readonly Error NotConnected = Error.Conflict(code: "Connector.NotConnected", description: "The connector is not connected");
}
