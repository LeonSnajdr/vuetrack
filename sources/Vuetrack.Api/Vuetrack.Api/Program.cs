try
{
    var builder = WebApplication.CreateBuilder(args);

    var app = builder.Build();

    app.UseHttpsRedirection();

    app.Run();
}
catch (Exception ex)
{
    Environment.ExitCode = 1;
}
