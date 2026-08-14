using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vuetrack.Api.Features.Integrations.Contracts;
using Vuetrack.Api.Infrastructure.Problems;

namespace Vuetrack.Api.Infrastructure.Validation;

public static class ModelBindingProblemFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var failures = context.ModelState
            .Where(entry => entry.Value is { ValidationState: ModelValidationState.Invalid })
            .Select(entry => (NormalizeKey(entry.Key), ToValidationError(entry.Value!)));

        var result = ProblemResults.Validation(context.HttpContext, failures);
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
