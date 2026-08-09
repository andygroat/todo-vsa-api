namespace Todo.VSA.Api.Features.Todos;

/// <summary>
/// Provides extension methods for registering to-do item endpoints in a WebApplication instance.
/// </summary>
internal static class RegisterTodoEndpoints
{
    /// <summary>
    /// Maps the endpoints related to to-do items in the WebApplication instance. This method can be used to configure the endpoints for creating, retrieving, updating, and deleting to-do items.
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoints.</param>
    /// <returns>The WebApplication instance with the mapped endpoints.</returns>
    public static WebApplication MapTodoEndpoints(this WebApplication app)
    {
        app.MapCreateTodoEndpoint();
        app.MapGetTodosEndpoint();
        app.MapGetTodoByIdEndpoint();

        return app;
    }
}
