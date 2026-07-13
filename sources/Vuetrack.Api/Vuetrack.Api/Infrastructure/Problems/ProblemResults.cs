using Microsoft.AspNetCore.Mvc;

namespace Vuetrack.Api.Infrastructure.Problems;

/// <summary>
/// Builds action results for RFC 9457 problem details, ensuring the mandated
/// <c>application/problem+json</c> content type is always set. Used by both the
/// FluentValidation action filter and the ErrorOr result mapping so every error
/// response shares an identical shape.
/// </summary>
public static class ProblemResults
{
    public static ObjectResult From(ProblemDetails problemDetails) =>
        new(problemDetails)
        {
            StatusCode = problemDetails.Status,
            ContentTypes = { "application/problem+json" },
        };
}
