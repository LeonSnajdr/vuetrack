using Samhammer.Options.Abstractions;

namespace Vuetrack.Api.Infrastructure.Logging;

[Option]
public class LogstashOptions
{
   public required string Url { get; set; }

   public required string ElasticIndex { get; set; }

   public required string Username { get; set; }

   public required string Password { get; set; }
}
