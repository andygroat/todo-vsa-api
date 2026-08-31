using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Api.Features.ShoppingLists;

/// <summary>
/// GetShoppingLists feature class for retrieving all shopping lists.
/// </summary>
public static class GetShoppingLists
{
    /// <summary>
    /// Query record for retrieving all shopping lists.
    /// </summary>
    public record Query : IRequest<Result<IEnumerable<Response>>>;

    /// <summary>
    /// Response record containing shopping list data.
    /// </summary>
    /// <param name="Id">The unique identifier of the shopping list.</param>
    /// <param name="Title">The title of the shopping list.</param>
    public record Response(Guid Id, string Title);

    /// <summary>
    /// Handler class for processing the GetShoppingLists query.
    /// </summary>
    /// <param name="context">The database context used to retrieve shopping lists.</param>
    /// <param name="logger">The logger used to log information about retrieving shopping lists.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<Query, Result<IEnumerable<Response>>>
    {
        public async Task<Result<IEnumerable<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var shoppingLists = await context.ShoppingLists
                .Where(sl => sl.Status != BusinessObjectStatus.Deleted)
                .Select(sl => new Response(sl.Id, sl.Title))
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved {Count} shopping lists", shoppingLists.Count);

            return Result.Success<IEnumerable<Response>>(shoppingLists);
        }
    }

    /// <summary>
    /// Maps the GetShoppingLists query to an HTTP GET endpoint at "/api/shoppinglists".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapGetShoppingListsEndpoint(this WebApplication app)
    {
        app.MapGet("/api/shoppinglists", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<IEnumerable<Response>> result = await mediator.Send(new Query(), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("GetShoppingLists")
        .WithTags("shoppinglists");

        return app;
    }
}
