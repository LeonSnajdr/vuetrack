using Microsoft.Extensions.DependencyInjection;

namespace Vuetrack.Suggestions.Engine;

public static class SuggestionEngineExtensions
{
    public static IServiceCollection AddSuggestionEngine(this IServiceCollection services)
    {
        return services.AddSingleton<ISuggestionEngine, SuggestionEngine>();
    }
}
