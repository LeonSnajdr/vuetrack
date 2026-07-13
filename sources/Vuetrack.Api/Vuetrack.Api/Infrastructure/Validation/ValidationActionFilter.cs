using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

            var modelState = new ModelStateDictionary();
            foreach (var failure in result.Errors)
            {
                modelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(context.HttpContext, modelState, StatusCodes.Status400BadRequest);

            context.Result = ProblemResults.From(problem);

            return;
        }

        await next();
    }
}
