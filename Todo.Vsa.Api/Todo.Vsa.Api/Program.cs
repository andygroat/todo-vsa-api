using Serilog;
using Todo.Vsa.Api.Infrastructure.Extensions;

// Configure Serilog as the logging provider for the application. Serilog is a structured logging library for .NET applications that allows for flexible and powerful logging capabilities, including support for various sinks (destinations) and structured log data.
// This will overwite the default logging configuration and enable Serilog to handle logging any issues with configuration.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{

    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    // Add application building blocks
    builder.AddApplicationBuilingBlocks();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.MapWebApplication();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    // Add exception handling middleware to the application, this is required for the custom exception handlers to work properly.
    app.UseExceptionHandler();

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

