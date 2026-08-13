using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vuetrack.Api.Features.Integrations.Contracts;
using Vuetrack.Api.Infrastructure.Problems;

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

            var failures = result.Errors.Select(failure => (failure.PropertyName, ToValidationError(failure.ErrorCode)));

            context.Result = ProblemResults.Validation(context.HttpContext, failures);

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
