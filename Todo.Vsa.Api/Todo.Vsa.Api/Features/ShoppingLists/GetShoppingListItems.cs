using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Api.Features.ShoppingLists;

/// <summary>
/// GetShoppingListItems feature class for retrieving all items in a shopping list.
/// </summary>
public static class GetShoppingListItems
{
    /// <summary>
    /// Query record for retrieving shopping list items.
    /// </summary>
    /// <param name="ListId">The ID of the shopping list.</param>
    public record Query(Guid ListId) : IRequest<Result<IEnumerable<Response>>>;

    /// <summary>
    /// Response record containing shopping list item data.
    /// </summary>
    /// <param name="Id">The unique identifier of the item.</param>
    /// <param name="Title">The title of the item.</param>
    /// <param name="IsComplete">Whether the item is complete.</param>
    public record Response(Guid Id, string Title, bool IsComplete);

    /// <summary>
    /// Handler class for processing the GetShoppingListItems query.
    /// </summary>
    /// <param name="context">The database context used to retrieve shopping list items.</param>
    /// <param name="logger">The logger used to log information.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<Query, Result<IEnumerable<Response>>>
    {
        public async Task<Result<IEnumerable<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Verify the shopping list exists and is not deleted
            var listExists = await context.ShoppingLists
                .AnyAsync(sl => sl.Id == request.ListId && sl.Status != BusinessObjectStatus.Deleted, cancellationToken);

            if (!listExists)
            {
                logger.LogWarning("ShoppingList {ShoppingListId} not found when retrieving items", request.ListId);
                return Result.Failure<IEnumerable<Response>>(new Error("ShoppingList.NotFound", $"ShoppingList with Id '{request.ListId}' was not found."));
            }

            var items = await context.ShoppingListItems
                .Where(sli => sli.ShoppingListId == request.ListId && sli.Status != BusinessObjectStatus.Deleted)
                .Select(sli => new Response(sli.Id, sli.Title, sli.IsComplete))
                .ToListAsync(cancellationToken);

            logger.LogInformation(
                "Retrieved {Count} items from ShoppingList {ShoppingListId}",
                items.Count, request.ListId);

            return Result.Success<IEnumerable<Response>>(items);
        }
    }

    /// <summary>
    /// Maps the GetShoppingListItems query to an HTTP GET endpoint at "/api/shoppinglists/{listId}/items".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapGetShoppingListItemsEndpoint(this WebApplication app)
    {
        app.MapGet("/api/shoppinglists/{listId:guid}/items", async (Guid listId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<IEnumerable<Response>> result = await mediator.Send(new Query(listId), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("GetShoppingListItems")
        .WithTags("shoppinglistitems");

        return app;
    }
}
