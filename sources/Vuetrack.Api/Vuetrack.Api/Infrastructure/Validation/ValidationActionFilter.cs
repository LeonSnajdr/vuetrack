using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vuetrack.Api.Infrastructure.Problems;
using Vuetrack.Backends.Abstractions.Contracts;

namespace Vuetrack.Api.Infrastructure.Validation;

public class ValidationActionFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    private IServiceProvider ServiceProvider { get; } = serviceProvider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (ServiceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (result.IsValid)
            {
                continue;
            }

            var modelState = new ModelStateDictionary();
            foreach (var failure in result.Errors)
            {
                var error = ToValidationError(failure.ErrorCode);
                modelState.AddModelError(failure.PropertyName, error);
            }

            var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(context.HttpContext, modelState, StatusCodes.Status400BadRequest);

            context.Result = ProblemResults.From(problem);

            return;
        }

        await next();
    }

    private static string ToValidationError(string errorCode)
    {
        if (Enum.TryParse<ValidationError>(errorCode, out var explicitError))
        {
            var explicitName = explicitError.ToString();
            return explicitName;
        }

        var error = errorCode switch
        {
            "NotEmptyValidator" or "NotNullValidator" => ValidationError.Required,
            _ => ValidationError.Invalid,
        };

        var name = error.ToString();
        return name;
    }
}
