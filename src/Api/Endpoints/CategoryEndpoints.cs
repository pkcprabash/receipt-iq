using Api.Contracts.Categories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categories").RequireAuthorization();

        group.MapGet("/", async (ReceiptIqDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var categories = await dbContext.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryResponse(c.Id, c.Name))
                .ToListAsync(cancellationToken);

            return Results.Ok(categories);
        });
    }
}
