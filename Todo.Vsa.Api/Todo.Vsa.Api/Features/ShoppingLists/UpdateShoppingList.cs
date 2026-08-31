using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Api.Features.ShoppingLists;

/// <summary>
/// UpdateShoppingList feature class for updating a shopping list's title.
/// </summary>
public static class UpdateShoppingList
{
    /// <summary>
    /// Command record representing the data required to update a shopping list.
    /// </summary>
    /// <param name="Title">The new title of the shopping list.</param>
    public record UpdateShoppingListCommand(string Title) : IRequest<Result<bool>>;

    /// <summary>
    /// Command record representing the data required to update a shopping list.
    /// </summary>
    /// <param name="Id">The unique identifier of the shopping list.</param>
    /// <param name="Title">The new title of the shopping list.</param>
    internal record Command(Guid Id, string Title) : IRequest<Result<bool>>;

    /// <summary>
    /// Validator class for validating the UpdateShoppingList command.
    /// </summary>
    public sealed class Validator : AbstractValidator<UpdateShoppingListCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>
    /// Handler class for processing the UpdateShoppingList command.
    /// </summary>
    /// <param name="context">The database context used to update the shopping list.</param>
    /// <param name="logger">The logger used to log information about the update.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<Command, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var shoppingList = await context.ShoppingLists
                .Where(sl => sl.Id == request.Id && sl.Status != BusinessObjectStatus.Deleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (shoppingList is null)
            {
                logger.LogWarning("ShoppingList {ShoppingListId} not found for update", request.Id);
                return Result.Failure<bool>(new Error("ShoppingList.NotFound", $"ShoppingList with Id '{request.Id}' was not found."));
            }

            shoppingList.Title = request.Title;
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Updated ShoppingList {ShoppingListId}: {ShoppingListTitle}",
                shoppingList.Id, shoppingList.Title);

            return Result.Success(true);
        }
    }

    /// <summary>
    /// Maps the UpdateShoppingList command to an HTTP PUT endpoint at "/api/shoppinglists/{id}".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapUpdateShoppingListEndpoint(this WebApplication app)
    {
        app.MapPut("/api/shoppinglists/{id:guid}", async (Guid id, UpdateShoppingListCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<bool> result = await mediator.Send(new Command(id, command.Title), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("UpdateShoppingList")
        .WithTags("shoppinglists");

        return app;
    }
}
