namespace Todo.Vsa.Api.Features.ShoppingLists;

/// <summary>
/// Provides extension methods for registering shopping list endpoints in a WebApplication instance.
/// </summary>
internal static class RegisterShoppingListEndpoints
{
    /// <summary>
    /// Maps the endpoints related to shopping lists and shopping list items in the WebApplication instance.
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoints.</param>
    /// <returns>The WebApplication instance with the mapped endpoints.</returns>
    public static WebApplication MapShoppingListEndpoints(this WebApplication app)
    {
        // Shopping list endpoints
        app.MapCreateShoppingListEndpoint();
        app.MapGetShoppingListsEndpoint();
        app.MapGetShoppingListByIdEndpoint();
        app.MapUpdateShoppingListEndpoint();
        app.MapDeleteShoppingListEndpoint();

        // Shopping list item endpoints
        app.MapCreateShoppingListItemEndpoint();
        app.MapGetShoppingListItemsEndpoint();
        app.MapGetShoppingListItemByIdEndpoint();
        app.MapUpdateShoppingListItemEndpoint();
        app.MapDeleteShoppingListItemEndpoint();

        return app;
    }
}
