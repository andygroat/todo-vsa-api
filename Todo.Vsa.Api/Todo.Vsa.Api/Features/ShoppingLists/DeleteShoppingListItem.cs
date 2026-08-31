using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Api.Features.ShoppingLists;

/// <summary>
/// DeleteShoppingListItem feature class for soft-deleting a shopping list item.
/// </summary>
public static class DeleteShoppingListItem
{
    /// <summary>
    /// Command record for deleting a shopping list item.
    /// </summary>
    /// <param name="ListId">The ID of the shopping list.</param>
    /// <param name="ItemId">The ID of the item to delete.</param>
    public record DeleteShoppingListItemCommand(Guid ListId, Guid ItemId) : IRequest<Result<bool>>;

    /// <summary>
    /// Handler class for processing the DeleteShoppingListItem command.
    /// </summary>
    /// <param name="context">The database context used to delete the shopping list item.</param>
    /// <param name="logger">The logger used to log information.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<DeleteShoppingListItemCommand, Result<bool>>
    {
        public async Task<Result<bool>> Handle(DeleteShoppingListItemCommand request, CancellationToken cancellationToken)
        {
            // Verify the shopping list exists and is not deleted
            var listExists = await context.ShoppingLists
                .AnyAsync(sl => sl.Id == request.ListId && sl.Status != BusinessObjectStatus.Deleted, cancellationToken);

            if (!listExists)
            {
                logger.LogWarning("ShoppingList {ShoppingListId} not found for item deletion", request.ListId);
                return Result.Failure<bool>(new Error("ShoppingList.NotFound", $"ShoppingList with Id '{request.ListId}' was not found."));
            }

            var item = await context.ShoppingListItems
                .Where(sli => sli.Id == request.ItemId 
                    && sli.ShoppingListId == request.ListId 
                    && sli.Status != BusinessObjectStatus.Deleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (item is null)
            {
                logger.LogWarning("ShoppingListItem {ItemId} not found for deletion", request.ItemId);
                return Result.Failure<bool>(new Error("ShoppingListItem.NotFound", $"ShoppingListItem with Id '{request.ItemId}' was not found."));
            }

            // Soft delete the item
            item.Status = BusinessObjectStatus.Deleted;
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Deleted ShoppingListItem {ItemId}", item.Id);

            return Result.Success(true);
        }
    }

    /// <summary>
    /// Maps the DeleteShoppingListItem command to an HTTP DELETE endpoint at "/api/shoppinglists/{listId}/items/{itemId}".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapDeleteShoppingListItemEndpoint(this WebApplication app)
    {
        app.MapDelete("/api/shoppinglists/{listId:guid}/items/{itemId:guid}", async (Guid listId, Guid itemId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<bool> result = await mediator.Send(new DeleteShoppingListItemCommand(listId, itemId), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("DeleteShoppingListItem")
        .WithTags("shoppinglistitems");

        return app;
    }
}
