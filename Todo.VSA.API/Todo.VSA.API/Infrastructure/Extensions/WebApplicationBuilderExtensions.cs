using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Todo.VSA.Api.Infrastructure.Behaviours;
using Todo.VSA.DataAccess.Context;

namespace Todo.VSA.Api.Infrastructure.Extensions
{
    internal static class WebApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds application building blocks to the WebApplicationBuilder.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder to add application building blocks to.</param>
        /// <returns>The WebApplicationBuilder with application building blocks added.</returns>
        public static WebApplicationBuilder AddApplicationBuilingBlocks(this WebApplicationBuilder builder)
        {
            // Add Serilog logging services
            builder.AddSerilogLogging();
            // Add Mediatr services
            builder.AddMedaitr();
            // Add Database context
            builder.AddDatabaseContext();

            return builder;
        }

        /// <summary>
        /// Adds Serilog logging services to the WebApplicationBuilder.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder to add Serilog logging services to.</param>
        /// <returns>The WebApplicationBuilder with Serilog logging services added.</returns>
        private static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
        {
            // Configure Serilog as the logging provider for the application. Serilog is a structured logging library for .NET applications that allows for flexible and powerful logging capabilities, including support for various sinks (destinations) and structured log data.
            builder.Services.AddSerilog();
            return builder;
        }

        /// <summary>
        /// Adds MediatR services and configures pipeline behaviors for logging and validation.
        /// MediatR is a popular library for implementing the mediator pattern in .NET applications, allowing for decoupled communication between components.
        /// This is like an event-driven architecture where requests and responses are handled through a mediator, promoting separation of concerns and maintainability.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder to add MediatR services to.</param>
        /// <returns>The WebApplicationBuilder with MediatR services added.</returns>
        private static WebApplicationBuilder AddMedaitr(this WebApplicationBuilder builder)
        {
            // Configure MediatR and register pipeline behaviors for logging and validation
            // https://www.nuget.org/packages/mediatr/
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });
            return builder;
        }

        private static WebApplicationBuilder AddDatabaseContext(this WebApplicationBuilder builder)
        {
            // Configure the database context for the application

            // Configure to use an in-memory database for development and testing purposes. This is useful for scenarios where you want to quickly set up a database without the need for an actual database server.
            builder.Services.AddDbContext<TodoDbContext>(options => options.UseInMemoryDatabase("TodoDb"));

            // In a production environment, you would typically configure the database context to use a real database provider (e.g., SQL Server, PostgreSQL, etc.) and provide the appropriate connection string from the configuration.
            // For example:
            // builder.Services.AddDbContext<TodoDbContext>(options =>
            //     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            return builder;
        }
    }
}
