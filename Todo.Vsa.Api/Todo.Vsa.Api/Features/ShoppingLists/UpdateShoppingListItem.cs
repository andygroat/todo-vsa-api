using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Api.Features.ShoppingLists;

/// <summary>
/// UpdateShoppingListItem feature class for updating a shopping list item.
/// </summary>
public static class UpdateShoppingListItem
{
    /// <summary>
    /// Command record representing the data required to update a shopping list item.
    /// </summary>
    /// <param name="ListId">The ID of the shopping list.</param>
    /// <param name="ItemId">The ID of the item to update.</param>
    /// <param name="Title">The new title of the item.</param>
    /// <param name="IsComplete">Whether the item is complete.</param>
    public record UpdateShoppingListItemCommand(string Title, bool IsComplete) : IRequest<Result<bool>>;

    /// <summary>
    /// Command record representing the data required to update a shopping list item.
    /// </summary>
    /// <param name="ListId">The ID of the shopping list.</param>
    /// <param name="ItemId">The ID of the item to update.</param>
    /// <param name="Title">The new title of the item.</param>
    /// <param name="IsComplete">Whether the item is complete.</param>
    internal record Command(Guid ListId, Guid ItemId, string Title, bool IsComplete) : IRequest<Result<bool>>;

    /// <summary>
    /// Validator class for validating the UpdateShoppingListItem command.
    /// </summary>
    public sealed class Validator : AbstractValidator<UpdateShoppingListItemCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler class for processing the UpdateShoppingListItem command.
    /// </summary>
    /// <param name="context">The database context used to update the shopping list item.</param>
    /// <param name="logger">The logger used to log information.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<Command, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Verify the shopping list exists and is not deleted
            var listExists = await context.ShoppingLists
                .AnyAsync(sl => sl.Id == request.ListId && sl.Status != BusinessObjectStatus.Deleted, cancellationToken);

            if (!listExists)
            {
                logger.LogWarning("ShoppingList {ShoppingListId} not found for update", request.ListId);
                return Result.Failure<bool>(new Error("ShoppingList.NotFound", $"ShoppingList with Id '{request.ListId}' was not found."));
            }

            var item = await context.ShoppingListItems
                .Where(sli => sli.Id == request.ItemId 
                    && sli.ShoppingListId == request.ListId 
                    && sli.Status != BusinessObjectStatus.Deleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (item is null)
            {
                logger.LogWarning("ShoppingListItem {ItemId} not found for update", request.ItemId);
                return Result.Failure<bool>(new Error("ShoppingListItem.NotFound", $"ShoppingListItem with Id '{request.ItemId}' was not found."));
            }

            item.Title = request.Title;
            item.IsComplete = request.IsComplete;
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Updated ShoppingListItem {ItemId}: {ItemTitle}, IsComplete={IsComplete}",
                item.Id, item.Title, item.IsComplete);

            return Result.Success(true);
        }
    }

    /// <summary>
    /// Maps the UpdateShoppingListItem command to an HTTP PUT endpoint at "/api/shoppinglists/{listId}/items/{itemId}".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapUpdateShoppingListItemEndpoint(this WebApplication app)
    {
        app.MapPut("/api/shoppinglists/{listId:guid}/items/{itemId:guid}", async (Guid listId, Guid itemId, UpdateShoppingListItemCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<bool> result = await mediator.Send(new Command(listId, itemId, command.Title, command.IsComplete), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("UpdateShoppingListItem")
        .WithTags("shoppinglistitems");

        return app;
    }
}
