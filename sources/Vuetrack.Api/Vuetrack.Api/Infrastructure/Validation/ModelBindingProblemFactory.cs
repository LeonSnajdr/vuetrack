using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vuetrack.Api.Infrastructure.Problems;
using Vuetrack.Backends.Abstractions.Contracts;

namespace Vuetrack.Api.Infrastructure.Validation;

public static class ModelBindingProblemFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var modelState = new ModelStateDictionary();

        foreach (var entry in context.ModelState)
        {
            if (entry.Value.ValidationState != ModelValidationState.Invalid)
            {
                continue;
            }

            var field = NormalizeKey(entry.Key);
            var error = ToValidationError(entry.Value);

            modelState.AddModelError(field, error);
        }

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(context.HttpContext, modelState, StatusCodes.Status400BadRequest);

        var result = ProblemResults.From(problem);
        return result;
    }

    private static string NormalizeKey(string key)
    {
        if (!key.StartsWith("$.", StringComparison.Ordinal))
        {
            return key;
        }

        var trimmed = key[2..];
        return trimmed;
    }

    private static string ToValidationError(ModelStateEntry entry)
    {
        var hasConversionError = entry.Errors.Any(error => error.Exception is not null);

        var validationError = hasConversionError ? ValidationError.Invalid : ValidationError.Required;

        var name = validationError.ToString();
        return name;
    }
}
