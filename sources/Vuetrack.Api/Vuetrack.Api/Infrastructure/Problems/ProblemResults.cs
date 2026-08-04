using Microsoft.AspNetCore.Mvc;

namespace Vuetrack.Api.Infrastructure.Problems;

public static class ProblemResults
{
    public static ObjectResult From(ProblemDetails problemDetails) =>
        new(problemDetails)
        {
            StatusCode = problemDetails.Status,
            ContentTypes = { "application/problem+json" },
        };
}
