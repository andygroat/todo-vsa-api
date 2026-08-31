using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;
using Todo.Vsa.Model.Domain.ShoppingLists;

namespace Todo.Vsa.Api.Features.ShoppingLists;

/// <summary>
/// CreateShoppingListItem feature class for adding an item to a shopping list.
/// </summary>
public static class CreateShoppingListItem
{
    /// <summary>
    /// Command record representing the data required to create a new shopping list item.
    /// </summary>
    /// <param name="Title">The title of the shopping list item.</param>
    public record CreateShoppingListItemCommand(string Title) : IRequest<Result<Guid>>;

    /// <summary>
    /// Command record representing the data required to create a new shopping list item.
    /// </summary>
    /// <param name="ListId">The ID of the shopping list to add the item to.</param>
    /// <param name="Title">The title of the shopping list item.</param>
    internal record Command(Guid ListId, string Title) : IRequest<Result<Guid>>;

    /// <summary>
    /// Validator class for validating the CreateShoppingListItem command.
    /// </summary>
    public sealed class Validator : AbstractValidator<CreateShoppingListItemCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler class for processing the CreateShoppingListItem command.
    /// </summary>
    /// <param name="context">The database context used to interact with shopping list items.</param>
    /// <param name="logger">The logger used to log information about the creation.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<Command, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Verify the parent shopping list exists and is not deleted
            var listExists = await context.ShoppingLists
                .AnyAsync(sl => sl.Id == request.ListId && sl.Status != BusinessObjectStatus.Deleted, cancellationToken);

            if (!listExists)
            {
                logger.LogWarning("ShoppingList {ShoppingListId} not found when creating item", request.ListId);
                return Result.Failure<Guid>(new Error("ShoppingList.NotFound", $"ShoppingList with Id '{request.ListId}' was not found."));
            }

            var item = new ShoppingListItem
            {
                Id = Guid.NewGuid(),
                ShoppingListId = request.ListId,
                Title = request.Title,
                IsComplete = false
            };

            context.ShoppingListItems.Add(item);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created ShoppingListItem {ItemId} in ShoppingList {ShoppingListId}: {ItemTitle}",
                item.Id, request.ListId, item.Title);

            return Result.Success(item.Id);
        }
    }

    /// <summary>
    /// Maps the CreateShoppingListItem command to an HTTP POST endpoint at "/api/shoppinglists/{listId}/items".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapCreateShoppingListItemEndpoint(this WebApplication app)
    {
        app.MapPost("/api/shoppinglists/{listId:guid}/items", async (Guid listId, CreateShoppingListItemCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await mediator.Send(new Command(listId, command.Title), cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/shoppinglists/{listId}/items/{result.Value}", new { id = result.Value })
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("CreateShoppingListItem")
        .WithTags("shoppinglistitems");

        return app;
    }
}
