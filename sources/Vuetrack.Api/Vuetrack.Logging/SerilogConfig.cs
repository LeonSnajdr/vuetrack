using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Samhammer.Logging.Logstash;
using Serilog;
using Vuetrack.Logging.Extensions;
using LoggingOptions = Vuetrack.Logging.Options.LoggingOptions;
using LogstashOptions = Vuetrack.Logging.Options.LogstashOptions;

namespace Vuetrack.Logging;

public static class SerilogConfig
{
    public const string ConsoleTemplate = "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {SourceContext} ==> {Message:lj}{NewLine}{Exception}";

    public static void ConfigureLogger(HostBuilderContext context, IServiceProvider services, LoggerConfiguration logger)
    {
        var environmentName = context.HostingEnvironment.EnvironmentName;
        var configuration = context.Configuration;

        var loggingOptions = services.GetRequiredService<IOptions<LoggingOptions>>().Value;
        var logstashOptions = services.GetRequiredService<IOptions<LogstashOptions>>().Value;

        const string fileTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u}] {ThreadId} {SourceContext} ==> {Message:lj}{NewLine}{Exception}";
        var filePath = Path.Combine(loggingOptions.LogFolder, loggingOptions.LogFile);

        logger.ReadFrom.Configuration(configuration);

        if (loggingOptions.EnableConsoleLogging)
        {
            logger.WriteTo.Console(outputTemplate: ConsoleTemplate);
        }

        if (loggingOptions.EnableElasticLogging)
        {
            logger.WriteTo.Logstash(logstashOptions.Url, logstashOptions.Username, logstashOptions.Password, logstashOptions.ElasticIndex);
        }

        if (loggingOptions.EnableFileLogging)
        {
            logger.WriteTo.File(filePath, rollingInterval: RollingInterval.Day, outputTemplate: fileTemplate);
        }

        logger
            .Enrich.WithProperty("EnvironmentName", environmentName)
            .Enrich.WithProperty("BranchName", ThisAssembly.Git.Branch)
            .Enrich.WithProperty("Brand", configuration.GetValue<string>("Brand"))
            .Enrich.WithExceptionStackTraceHash()
            .Enrich.WithThreadId()
            .Enrich.WithAssemblyName()
            .Enrich.WithAssemblyVersion()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.FromLogContext()
            .Enrich.WithUserIdentity();
    }
}
