using System.Security.Claims;
using Api.Contracts.Categories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Endpoints;

public static class CategoryEndpoints
{
    private const int MaxNameLength = 50;

    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categories").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var categories = await dbContext.Categories
                .Where(c => c.UserId == null || c.UserId == userId)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryResponse(c.Id, c.Name, c.UserId == null))
                .ToListAsync(cancellationToken);

            return Results.Ok(categories);
        });

        group.MapPost("/", async (
            CategoryRequest request,
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var (name, error) = await ValidateNameAsync(request.Name, userId, excludeId: null, dbContext, cancellationToken);
            if (error is not null)
            {
                return error;
            }

            var category = new Category { Name = name!, UserId = userId };
            dbContext.Categories.Add(category);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Results.Conflict(new { message = "A category with this name already exists." });
            }

            return Results.Created($"/categories/{category.Id}", new CategoryResponse(category.Id, category.Name, false));
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            CategoryRequest request,
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var (category, notOwned) = await FindVisibleCategoryAsync(id, userId, dbContext, cancellationToken);
            if (notOwned is not null)
            {
                return notOwned;
            }

            var (name, error) = await ValidateNameAsync(request.Name, userId, excludeId: id, dbContext, cancellationToken);
            if (error is not null)
            {
                return error;
            }

            category!.Name = name!;

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Results.Conflict(new { message = "A category with this name already exists." });
            }

            return Results.Ok(new CategoryResponse(category.Id, category.Name, false));
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var (category, notOwned) = await FindVisibleCategoryAsync(id, userId, dbContext, cancellationToken);
            if (notOwned is not null)
            {
                return notOwned;
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Items fall back to Uncategorized rather than being left with no category at all.
            // Only the owner can have assigned a custom category, so this can't touch anyone else's items.
            await dbContext.ReceiptLineItems
                .Where(li => li.CategoryId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(li => li.CategoryId, SystemCategories.UncategorizedId), cancellationToken);

            // Rules pointing at this category are removed with it (cascade).
            dbContext.Categories.Remove(category!);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.NoContent();
        });
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    // Other users' categories look like they don't exist; system ones exist but can't be changed.
    private static async Task<(Category? Category, IResult? Failure)> FindVisibleCategoryAsync(
        Guid id, Guid userId, ReceiptIqDbContext dbContext, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id && (c.UserId == null || c.UserId == userId), cancellationToken);

        if (category is null)
        {
            return (null, Results.NotFound());
        }

        if (category.IsSystem)
        {
            return (null, Results.Json(new { message = "System categories can't be changed." }, statusCode: StatusCodes.Status403Forbidden));
        }

        return (category, null);
    }

    private static async Task<(string? Name, IResult? Error)> ValidateNameAsync(
        string? rawName, Guid userId, Guid? excludeId, ReceiptIqDbContext dbContext, CancellationToken cancellationToken)
    {
        var name = rawName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return (null, Results.BadRequest(new { message = "Name is required." }));
        }

        if (name.Length > MaxNameLength)
        {
            return (null, Results.BadRequest(new { message = $"Name must be at most {MaxNameLength} characters." }));
        }

        var lowered = name.ToLower();
        var taken = await dbContext.Categories.AnyAsync(
            c => c.Id != excludeId && (c.UserId == null || c.UserId == userId) && c.Name.ToLower() == lowered,
            cancellationToken);

        return taken
            ? (null, Results.Conflict(new { message = "A category with this name already exists." }))
            : (name, null);
    }
}
