using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using Todo.VSA.Api.Infrastructure.Behaviours;
using Todo.VSA.Api.Infrastructure.Exceptions;
using Todo.VSA.DataAccess.Context;

namespace Todo.VSA.Api.Infrastructure.Extensions
{
    [ExcludeFromCodeCoverage]
    internal static class WebApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds application building blocks to the WebApplicationBuilder.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder to add application building blocks to.</param>
        /// <returns>The WebApplicationBuilder with application building blocks added.</returns>
        public static WebApplicationBuilder AddApplicationBuilingBlocks(this WebApplicationBuilder builder)
        {
            // Add FluentValidation validators
            builder.AddFluentValidationValidators();
            // Add Serilog logging services
            builder.AddSerilogLogging();
            // Add Mediatr services
            builder.AddMedaitr();
            // Add Database context
            builder.AddDatabaseContext();
            // Add Exception handling middleware
            builder.AddExceptionHandling();

            return builder;
        }

        /// <summary>
        /// Adds Serilog logging services to the WebApplicationBuilder.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder to add FluentValidation validators to.</param>
        /// <returns>The WebApplicationBuilder with FluentValidation validators added.</returns>
        private static WebApplicationBuilder AddFluentValidationValidators(this WebApplicationBuilder builder)
        {
            // Configure FluentValidation validators for the application. FluentValidation is a popular library for building strongly-typed validation rules for .NET applications.
            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
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

        /// <summary>
        /// Adds the database context for the application, configuring it to use an in-memory database for development and testing purposes. In a production environment, you would typically configure the database context to use a real database provider (e.g., SQL Server, PostgreSQL, etc.) and provide the appropriate connection string from the configuration.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder to add the database context to.</param>
        /// <returns>The WebApplicationBuilder with the database context added.</returns>
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

        /// <summary>
        /// Adds exception handling middleware to the application. This middleware is responsible for catching unhandled exceptions that occur during the processing of HTTP requests and generating appropriate error responses. It can be used to provide consistent error handling and logging throughout the application.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder to add the exception handling middleware to.</param>
        /// <returns>The WebApplicationBuilder with the exception handling middleware added.</returns>
        private static WebApplicationBuilder AddExceptionHandling(this WebApplicationBuilder builder)
        {
            // Configure the validation exception handler to handle FluentValidation exceptions and generate appropriate error responses. This is useful for scenarios where you want to provide detailed information about validation errors to clients in a standardized format.
            builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

            // Configure the global exception handler to handle unhandled exceptions and generate appropriate error responses. This is a global exception handler that will catch any unhandled exceptions that occur during the processing of HTTP requests and provide a standardized error response to clients. Any specific exception handlers (like ValidationExceptionHandler) should be registered before the global handler.
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            // Add ProblemDetails middleware to the application. ProblemDetails is a standardized format for representing error responses in HTTP APIs, as defined by RFC 9457. It provides a consistent way to convey error information to clients, including details about the error type, status code, and additional context.
            // If this line is not included, the dependency injection container will not be able to resolve the IProblemDetailsService, which is required by the exception handlers to generate ProblemDetails responses.
            builder.Services.AddProblemDetails();

            return builder;
        }
    }
}
