using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vuetrack.Api.Infrastructure.Problems;

namespace Vuetrack.Api.Infrastructure.Validation;

public static class ValidationErrorOr
{
    extension(ControllerBase controller)
    {
        public IActionResult ToActionResult<T>(ErrorOr<T> result) => result.Match<IActionResult>(value => controller.Ok(value), controller.ToProblem);

        public IActionResult ToActionResult(ErrorOr<Success> result) => result.Match(_ => controller.NoContent(), controller.ToProblem);

        public IActionResult ToActionResult(ErrorOr<Created> result) => result.Match(_ => controller.StatusCode(StatusCodes.Status201Created), controller.ToProblem);

        public IActionResult ToActionResult(ErrorOr<Updated> result) => result.Match(_ => controller.NoContent(), controller.ToProblem);

        public IActionResult ToActionResult(ErrorOr<Deleted> result) => result.Match(_ => controller.NoContent(), controller.ToProblem);

        private IActionResult ToProblem(List<Error> errors)
        {
            var factory = controller.ProblemDetailsFactory;
            var httpContext = controller.HttpContext;

            if (errors.Count == 0)
            {
                return ProblemResults.From(factory.CreateProblemDetails(httpContext, StatusCodes.Status500InternalServerError));
            }

            if (errors.All(e => e.Type == ErrorType.Validation))
            {
                var modelState = new ModelStateDictionary();
                foreach (var error in errors)
                {
                    modelState.AddModelError(error.Code, error.Description);
                }

                return ProblemResults.From(factory.CreateValidationProblemDetails(httpContext, modelState, StatusCodes.Status400BadRequest));
            }

            var firstError = errors[0];
            var statusCode = firstError.Type switch
            {
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError,
            };

            return ProblemResults.From(factory.CreateProblemDetails(httpContext, statusCode, detail: firstError.Description));
        }
    }
}
