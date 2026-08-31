using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Api.Features.ShoppingLists;

/// <summary>
/// GetShoppingListById feature class for retrieving a shopping list by ID.
/// </summary>
public static class GetShoppingListById
{
    /// <summary>
    /// Query record for retrieving a shopping list by ID.
    /// </summary>
    /// <param name="Id">The unique identifier of the shopping list.</param>
    public record Query(Guid Id) : IRequest<Result<Response>>;

    /// <summary>
    /// Response record containing shopping list data.
    /// </summary>
    /// <param name="Id">The unique identifier of the shopping list.</param>
    /// <param name="Title">The title of the shopping list.</param>
    public record Response(Guid Id, string Title);

    /// <summary>
    /// Handler class for processing the GetShoppingListById query.
    /// </summary>
    /// <param name="context">The database context used to retrieve the shopping list.</param>
    /// <param name="logger">The logger used to log information about retrieving the shopping list.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var shoppingList = await context.ShoppingLists
                .Where(sl => sl.Id == request.Id && sl.Status != BusinessObjectStatus.Deleted)
                .Select(sl => new Response(sl.Id, sl.Title))
                .FirstOrDefaultAsync(cancellationToken);

            if (shoppingList is null)
            {
                logger.LogWarning("ShoppingList {ShoppingListId} not found", request.Id);
                return Result.Failure<Response>(new Error("ShoppingList.NotFound", $"ShoppingList with Id '{request.Id}' was not found."));
            }

            logger.LogInformation("Retrieved ShoppingList {ShoppingListId}", request.Id);

            return Result<Response>.Success(shoppingList);
        }
    }

    /// <summary>
    /// Maps the GetShoppingListById query to an HTTP GET endpoint at "/api/shoppinglists/{id}".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapGetShoppingListByIdEndpoint(this WebApplication app)
    {
        app.MapGet("/api/shoppinglists/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<Response> result = await mediator.Send(new Query(id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("GetShoppingListById")
        .WithTags("shoppinglists");

        return app;
    }
}
