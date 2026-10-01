using System.Security.Claims;
using Api.Contracts.Budgets;
using Application.Budgets;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Endpoints;

public static class BudgetEndpoints
{
    private const decimal MaxMonthlyLimit = 10_000_000_000_000m;

    public static void MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/budgets").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var budgets = await dbContext.Budgets
                .Where(b => b.UserId == userId)
                .Include(b => b.Category)
                .ToListAsync(cancellationToken);

            if (budgets.Count == 0)
            {
                return Results.Ok(Array.Empty<BudgetResponse>());
            }

            var (monthStart, monthEnd) = CurrentMonthRange();

            // One pass over this month's spend, grouped by category, covers every budget
            // (including the "overall" one, which sums across all of them).
            var spendByCategory = await AnalyticsEndpoints.SpendLineItems(dbContext, userId, monthStart, monthEnd)
                .GroupBy(li => li.CategoryId)
                .Select(g => new { CategoryId = g.Key, Total = g.Sum(li => li.Amount) })
                .ToListAsync(cancellationToken);

            var overallSpend = spendByCategory.Sum(c => c.Total);
            var spendByCategoryId = spendByCategory
                .Where(c => c.CategoryId.HasValue)
                .ToDictionary(c => c.CategoryId!.Value, c => c.Total);

            var responses = budgets
                .Select(b =>
                {
                    var spend = b.CategoryId is { } categoryId ? spendByCategoryId.GetValueOrDefault(categoryId) : overallSpend;
                    var percentUsed = b.MonthlyLimit > 0 ? (double)(spend / b.MonthlyLimit) * 100 : 100;
                    return new BudgetResponse(
                        b.Id,
                        b.CategoryId,
                        b.Category?.Name ?? "Overall",
                        b.MonthlyLimit,
                        spend,
                        percentUsed,
                        BudgetAlertPolicy.DetermineStatus(spend, b.MonthlyLimit).ToString());
                })
                .OrderByDescending(b => b.PercentUsed)
                .ToList();

            return Results.Ok(responses);
        });

        group.MapPost("/", async (
            BudgetRequest request,
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var limitError = ValidateLimit(request.MonthlyLimit);
            if (limitError is not null)
            {
                return limitError;
            }

            if (request.CategoryId.HasValue)
            {
                var categoryVisible = await dbContext.Categories
                    .AnyAsync(c => c.Id == request.CategoryId && (c.UserId == null || c.UserId == userId), cancellationToken);
                if (!categoryVisible)
                {
                    return Results.BadRequest(new { message = "Unknown category." });
                }
            }

            var budget = new Budget
            {
                UserId = userId,
                CategoryId = request.CategoryId,
                MonthlyLimit = decimal.Round(request.MonthlyLimit, 2, MidpointRounding.AwayFromZero)
            };
            dbContext.Budgets.Add(budget);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Results.Conflict(new
                {
                    message = request.CategoryId.HasValue
                        ? "A budget for this category already exists."
                        : "An overall budget already exists."
                });
            }

            return Results.Created($"/budgets/{budget.Id}", new { budget.Id });
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            BudgetRequest request,
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var limitError = ValidateLimit(request.MonthlyLimit);
            if (limitError is not null)
            {
                return limitError;
            }

            var budget = await dbContext.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken);
            if (budget is null)
            {
                return Results.NotFound();
            }

            // The category a budget is scoped to doesn't change — only the limit does.
            // Changing scope is deleting one budget and creating another.
            budget.MonthlyLimit = decimal.Round(request.MonthlyLimit, 2, MidpointRounding.AwayFromZero);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
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

            var budget = await dbContext.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken);
            if (budget is null)
            {
                return Results.NotFound();
            }

            dbContext.Budgets.Remove(budget);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static IResult? ValidateLimit(decimal monthlyLimit)
    {
        if (monthlyLimit <= 0)
        {
            return Results.BadRequest(new { message = "Monthly limit must be greater than zero." });
        }

        if (monthlyLimit >= MaxMonthlyLimit)
        {
            return Results.BadRequest(new { message = "Monthly limit is out of range." });
        }

        return null;
    }

    // Budgets are tracked against the current calendar month, in the server's own clock —
    // the same basis the fake extractor and receipt processing already use.
    private static (DateOnly Start, DateOnly End) CurrentMonthRange()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = new DateOnly(today.Year, today.Month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }
}
