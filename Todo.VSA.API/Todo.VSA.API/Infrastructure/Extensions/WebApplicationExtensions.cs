using Scalar.AspNetCore;

namespace Todo.VSA.Api.Infrastructure.Extensions
{
    internal static class WebApplicationExtensions
    {
        /// <summary>
        /// Maps the WebApplication instance and returns it. This method can be used to configure additional middleware, endpoints, or other application-specific settings.
        /// </summary>
        /// <param name="app">The WebApplication instance to map.</param>
        /// <returns>The mapped WebApplication instance.</returns>
        public static WebApplication MapWebApplication(this WebApplication app)
        {
            // If the application is in development environment, map the OpenAPI endpoints and the Scalar API reference for the WebApplication instance.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApiWithScalarPage();
            }

            return app;
        }

        /// <summary>
        /// Maps the OpenAPI endpoints and the Scalar API reference for the WebApplication instance. This method can be used to configure OpenAPI documentation and Scalar API reference for the application.
        /// </summary>
        /// <param name="app">The WebApplication instance to map.</param>
        /// <returns>The mapped WebApplication instance.</returns>
        public static WebApplication MapOpenApiWithScalarPage(this WebApplication app)
        {
            app.MapOpenApi();
            app.MapScalarApiReference();

            return app;
        }
    }
}
