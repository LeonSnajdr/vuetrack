using System.Security.Claims;

namespace Vuetrack.Api.Infrastructure.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        return principal.Identity?.Name ?? throw new InvalidOperationException("Authenticated user has no name.");
    }
}
