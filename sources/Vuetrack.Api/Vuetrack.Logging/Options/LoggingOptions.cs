using Samhammer.Options.Abstractions;

namespace Vuetrack.Logging.Options;

[Option]
public class LoggingOptions
{
   public required string LogFile { get; set; }

   public required string LogFolder { get; set; }

   public bool EnableElasticLogging { get; set; }

   public bool EnableConsoleLogging { get; set; }

   public bool EnableFileLogging { get; set; }
}
