using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Vuetrack.Api.Infrastructure.ModelBinding;

public class TrimStringModelBinderProvider : IModelBinderProvider
{
    private static readonly IModelBinder Binder = new TrimStringModelBinder();

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.BindingInfo.BindingSource == BindingSource.Body)
        {
            return null;
        }

        return context.Metadata.ModelType == typeof(string) ? Binder : null;
    }
}
