using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Api.Features.ShoppingLists;

/// <summary>
/// GetShoppingListItemById feature class for retrieving a specific shopping list item.
/// </summary>
public static class GetShoppingListItemById
{
    /// <summary>
    /// Query record for retrieving a shopping list item by ID.
    /// </summary>
    /// <param name="ListId">The ID of the shopping list.</param>
    /// <param name="ItemId">The ID of the item.</param>
    public record Query(Guid ListId, Guid ItemId) : IRequest<Result<Response>>;

    /// <summary>
    /// Response record containing shopping list item data.
    /// </summary>
    /// <param name="Id">The unique identifier of the item.</param>
    /// <param name="Title">The title of the item.</param>
    /// <param name="IsComplete">Whether the item is complete.</param>
    public record Response(Guid Id, string Title, bool IsComplete);

    /// <summary>
    /// Handler class for processing the GetShoppingListItemById query.
    /// </summary>
    /// <param name="context">The database context used to retrieve the shopping list item.</param>
    /// <param name="logger">The logger used to log information.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Verify the shopping list exists and is not deleted
            var listExists = await context.ShoppingLists
                .AnyAsync(sl => sl.Id == request.ListId && sl.Status != BusinessObjectStatus.Deleted, cancellationToken);

            if (!listExists)
            {
                logger.LogWarning("ShoppingList {ShoppingListId} not found", request.ListId);
                return Result.Failure<Response>(new Error("ShoppingList.NotFound", $"ShoppingList with Id '{request.ListId}' was not found."));
            }

            var item = await context.ShoppingListItems
                .Where(sli => sli.Id == request.ItemId 
                    && sli.ShoppingListId == request.ListId 
                    && sli.Status != BusinessObjectStatus.Deleted)
                .Select(sli => new Response(sli.Id, sli.Title, sli.IsComplete))
                .FirstOrDefaultAsync(cancellationToken);

            if (item is null)
            {
                logger.LogWarning("ShoppingListItem {ItemId} not found in ShoppingList {ShoppingListId}", request.ItemId, request.ListId);
                return Result.Failure<Response>(new Error("ShoppingListItem.NotFound", $"ShoppingListItem with Id '{request.ItemId}' was not found."));
            }

            logger.LogInformation("Retrieved ShoppingListItem {ItemId}", request.ItemId);

            return Result.Success(item);
        }
    }

    /// <summary>
    /// Maps the GetShoppingListItemById query to an HTTP GET endpoint at "/api/shoppinglists/{listId}/items/{itemId}".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapGetShoppingListItemByIdEndpoint(this WebApplication app)
    {
        app.MapGet("/api/shoppinglists/{listId:guid}/items/{itemId:guid}", async (Guid listId, Guid itemId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<Response> result = await mediator.Send(new Query(listId, itemId), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("GetShoppingListItemById")
        .WithTags("shoppinglistitems");

        return app;
    }
}
