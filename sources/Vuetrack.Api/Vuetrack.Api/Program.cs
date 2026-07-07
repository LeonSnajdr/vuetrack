using Samhammer.Swagger.Versioning;
using Samhammer.Web.Common.Extensions;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllers();

    builder.Services.AddApiVersioning();

    builder.Services.AddSwaggerGen();
    builder.Services.AddSwaggerVersionedApi();

    builder.Services.AddHttpClient();
    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    app.UseRouting();

    app.UseVersion();

    app.UseSwagger();

    app.UseSwaggerUI();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
