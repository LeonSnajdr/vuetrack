using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Vuetrack.Api.Infrastructure.Problems;

public static class ProblemResults
{
    public static ObjectResult From(ProblemDetails problemDetails) =>
        new(problemDetails)
        {
            StatusCode = problemDetails.Status,
            ContentTypes = { "application/problem+json" },
        };

    public static ObjectResult Validation(HttpContext httpContext, ProblemDetailsFactory factory, IEnumerable<(string Field, string Error)> failures)
    {
        var modelState = new ModelStateDictionary();
        foreach (var failure in failures)
        {
            modelState.AddModelError(failure.Field, failure.Error);
        }

        var problem = factory.CreateValidationProblemDetails(httpContext, modelState, StatusCodes.Status400BadRequest);

        return From(problem);
    }

    public static ObjectResult Validation(HttpContext httpContext, IEnumerable<(string Field, string Error)> failures)
    {
        var factory = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        return Validation(httpContext, factory, failures);
    }
}
