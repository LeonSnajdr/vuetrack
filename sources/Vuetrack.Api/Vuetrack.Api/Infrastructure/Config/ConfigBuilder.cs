using ConfigurationSubstitution;
using Samhammer.Configuration.Childs;

namespace Vuetrack.Api.Infrastructure.Config;

public static class ConfigBuilder
{
    public static ConfigurationManager AddAppConfiguration(this ConfigurationManager configManager, IWebHostEnvironment webHostEnvironment)
    {
        var environment = webHostEnvironment.EnvironmentName;
        configManager
            .AddJsonFile("appsettings.user.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.{ThisAssembly.Git.Branch}.json", optional: true)
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>("environment", environment.GetShortEnvironmentName()),
                new KeyValuePair<string, string?>("-branch", GetBranchSuffix(webHostEnvironment, "-")),
                new KeyValuePair<string, string?>("branch", ThisAssembly.Git.Branch)
            ])
            .EnableSubstitutions()
            .EnableChildSubstitutions(Environment.GetEnvironmentVariable("Brand"))
            .EnableSubstitutions();

        return configManager;
    }

    private static string GetShortEnvironmentName(this string environment)
    {
        return environment switch
        {
            "Development" => "dev",
            "Staging" => "staging",
            "Production" => "prod",
            _ => environment.ToLower()
        };
    }

    private static string GetBranchSuffix(IHostEnvironment environment, string separator)
    {
        return environment.IsProduction() ? string.Empty : $"{separator}{ThisAssembly.Git.Branch}";
    }
}
