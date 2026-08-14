using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Vuetrack.Api.Infrastructure.ModelBinding;

public class TrimStringModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        bindingContext.Result = ModelBindingResult.Success(valueProviderResult.FirstValue?.Trim());

        return Task.CompletedTask;
    }
}
