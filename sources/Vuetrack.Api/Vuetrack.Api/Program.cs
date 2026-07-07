using Samhammer.DependencyInjection;
using Samhammer.Options;
using Samhammer.Swagger.Versioning;
using Samhammer.Web.Common.Extensions;
using Serilog;
using Vuetrack.Logging;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: SerilogConfig.ConsoleTemplate)
    .CreateBootstrapLogger();

Log.Information("Application starting");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseSerilog(SerilogConfig.ConfigureLogger);

    builder.Services.ResolveOptions(builder.Configuration);
    builder.Services.ResolveDependencies();

    builder.Services.AddControllers();

    builder.Services.AddApiVersioning();

    builder.Services.AddSwaggerGen();
    builder.Services.AddSwaggerVersionedApi();

    builder.Services.AddHttpClient();
    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    app.UseDefaultExceptionHandler();

    app.UseRouting();

    app.UseVersion();

    app.UseSwagger();

    app.UseSwaggerUI();

    app.UseSerilogRequestLogging();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
