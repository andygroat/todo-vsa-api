using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Api.Features.ShoppingLists;

/// <summary>
/// DeleteShoppingList feature class for soft-deleting a shopping list and its items.
/// </summary>
public static class DeleteShoppingList
{
    /// <summary>
    /// Command record for deleting a shopping list.
    /// </summary>
    /// <param name="Id">The unique identifier of the shopping list to delete.</param>
    public record DeleteShoppingListCommand(Guid Id) : IRequest<Result<bool>>;

    /// <summary>
    /// Handler class for processing the DeleteShoppingList command.
    /// </summary>
    /// <param name="context">The database context used to delete the shopping list.</param>
    /// <param name="logger">The logger used to log information about the deletion.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<DeleteShoppingListCommand, Result<bool>>
    {
        public async Task<Result<bool>> Handle(DeleteShoppingListCommand request, CancellationToken cancellationToken)
        {
            var shoppingList = await context.ShoppingLists
                .Include(sl => sl.Items)
                .Where(sl => sl.Id == request.Id && sl.Status != BusinessObjectStatus.Deleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (shoppingList is null)
            {
                logger.LogWarning("ShoppingList {ShoppingListId} not found for deletion", request.Id);
                return Result.Failure<bool>(new Error("ShoppingList.NotFound", $"ShoppingList with Id '{request.Id}' was not found."));
            }

            // Soft delete the list
            shoppingList.Status = BusinessObjectStatus.Deleted;

            // Soft delete all items
            foreach (var item in shoppingList.Items)
            {
                item.Status = BusinessObjectStatus.Deleted;
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Deleted ShoppingList {ShoppingListId} and {ItemCount} items",
                shoppingList.Id, shoppingList.Items.Count);

            return Result.Success(true);
        }
    }

    /// <summary>
    /// Maps the DeleteShoppingList command to an HTTP DELETE endpoint at "/api/shoppinglists/{id}".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapDeleteShoppingListEndpoint(this WebApplication app)
    {
        app.MapDelete("/api/shoppinglists/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<bool> result = await mediator.Send(new DeleteShoppingListCommand(id), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("DeleteShoppingList")
        .WithTags("shoppinglists");

        return app;
    }
}
