using ErrorOr;

namespace Vuetrack.Framework.Errors;

public static class BackendError
{
    public static readonly Error NotConnected = Error.Conflict(code: "Backend.NotConnected", description: "The backend is not connected");
}
