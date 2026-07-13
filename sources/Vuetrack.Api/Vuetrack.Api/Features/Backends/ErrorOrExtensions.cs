using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Vuetrack.Api.Features.Backends;

public static class ErrorOrExtensions
{
    public static IActionResult ToActionResult<T>(this ErrorOr<T> result) =>
        result.Match(value => new OkObjectResult(value), ToProblem);

    public static IActionResult ToActionResult(this ErrorOr<Success> result) =>
        result.Match(_ => (IActionResult)new NoContentResult(), ToProblem);

    private static IActionResult ToProblem(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            var validationProblem = new ValidationProblemDetails(
                errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
            };

            return new ObjectResult(validationProblem) { StatusCode = validationProblem.Status };
        }

        var error = errors[0];
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Description,
        };

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
