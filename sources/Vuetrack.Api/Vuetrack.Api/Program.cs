using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.Extensions.Options;
using Samhammer.Authentication.Api.Jwt;
using Samhammer.Authentication.Api.Keycloak;
using Samhammer.DependencyInjection;
using Samhammer.Mongo;
using Samhammer.Options;
using Samhammer.Swagger.Authentication;
using Samhammer.Swagger.Versioning;
using Samhammer.Web.Common.Extensions;
using Serilog;
using Vuetrack.Api.Features.Suggestions.Engine;
using Vuetrack.Api.Infrastructure.Config;
using Vuetrack.Api.Infrastructure.Cors;
using Vuetrack.Api.Infrastructure.Validation;
using Vuetrack.Backends.Timetracking;
using Vuetrack.Backends.Timetracking.Api;
using Vuetrack.Logging;
using ZiggyCreatures.Caching.Fusion;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: SerilogConfig.ConsoleTemplate)
    .CreateBootstrapLogger();

Log.Information("Application starting");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddAppConfiguration(builder.Environment);

    builder.Logging.ClearProviders();
    builder.Host.UseSerilog(SerilogConfig.ConfigureLogger);

    builder.Services.ResolveOptions(builder.Configuration);
    builder.Services.ResolveDependencies();

    builder.Services.AddJwtAuthentication().AddKeycloak(builder.Configuration);

    builder.Services.AddProblemDetails();

    builder.Services.AddScoped<ValidationActionFilter>();
    builder.Services
        .AddControllers(options => options.Filters.Add<ValidationActionFilter>())
        .AddJsonOptions(opts =>
        {
            var enumConverter = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
            opts.JsonSerializerOptions.Converters.Add(enumConverter);
        });

    builder.Services.AddMongoDb(builder.Configuration);

    builder.Services.AddDataProtection();

    builder.Services.AddFusionCache();

    builder.Services.AddApiVersioning(o =>
    {
        o.ReportApiVersions = true;
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.DefaultApiVersion = new ApiVersion(1);
    });

    builder.Services.ConfigureOptions<ConfigureCors>();

    builder.Services.AddSwaggerGen();
    builder.Services.AddSwaggerAuthentication(builder.Configuration);
    builder.Services.AddSwaggerVersionedApi();

    builder.Services.AddHttpClient();
    builder.Services.AddHttpClient<ITimetrackingApiClient, TimetrackingApiClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<TimetrackingOptions>>().Value;
        var baseUrl = options.ApiBaseUrl.TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);
    });
    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    app.UseDefaultExceptionHandler();

    app.UseRouting();

    app.UseVersion();

    app.UseSwagger();

    app.UseSwaggerUI();

    app.UseAuthentication();

    app.UseSerilogRequestLogging();

    app.UseAuthorization();

    app.UseCors();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
