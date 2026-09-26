using System.Security.Claims;
using Api.Contracts.Analytics;
using Application.Analytics;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/analytics").RequireAuthorization();

        group.MapGet("/spend-by-category", async (
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken,
            DateOnly? from = null,
            DateOnly? to = null) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            if (from.HasValue && to.HasValue && from > to)
            {
                return Results.BadRequest(new { message = "'from' must not be after 'to'." });
            }

            // Items with no category count as Uncategorized so the breakdown always adds up to the total.
            var totals = await SpendLineItems(dbContext, userId, from, to)
                .GroupBy(li => li.CategoryId ?? SystemCategories.UncategorizedId)
                .Select(g => new { CategoryId = g.Key, Total = g.Sum(li => li.Amount), Count = g.Count() })
                .ToListAsync(cancellationToken);

            var names = await dbContext.Categories
                .Where(c => c.UserId == null || c.UserId == userId)
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

            var categories = totals
                .Select(t => new CategorySpendResponse(t.CategoryId, names.GetValueOrDefault(t.CategoryId, "Uncategorized"), t.Total, t.Count))
                .OrderByDescending(c => c.TotalAmount)
                .ThenBy(c => c.CategoryName)
                .ToList();

            return Results.Ok(new SpendByCategoryResponse(categories.Sum(c => c.TotalAmount), categories));
        });

        group.MapGet("/spend-by-month", async (
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken,
            DateOnly? from = null,
            DateOnly? to = null) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            if (from.HasValue && to.HasValue && from > to)
            {
                return Results.BadRequest(new { message = "'from' must not be after 'to'." });
            }

            var totals = await SpendLineItems(dbContext, userId, from, to)
                .GroupBy(li => new { li.Receipt!.PurchaseDate!.Value.Year, li.Receipt.PurchaseDate.Value.Month })
                .Select(g => new MonthTotal(
                    g.Key.Year,
                    g.Key.Month,
                    g.Sum(li => li.Amount),
                    g.Select(li => li.ReceiptId).Distinct().Count()))
                .ToListAsync(cancellationToken);

            if (totals.Count == 0 && !(from.HasValue && to.HasValue))
            {
                return Results.Ok(new SpendByMonthResponse(0m, []));
            }

            // An open-ended range runs out to the first/last month that actually has spending.
            var rangeStart = from ?? totals.Min(t => new DateOnly(t.Year, t.Month, 1));
            var rangeEnd = to ?? totals.Max(t => new DateOnly(t.Year, t.Month, 1));

            if (MonthSeries.CountMonths(rangeStart, rangeEnd) > MonthSeries.MaxMonths)
            {
                return Results.BadRequest(new { message = $"Range must span at most {MonthSeries.MaxMonths} months." });
            }

            var months = MonthSeries.FillGaps(totals, rangeStart, rangeEnd)
                .Select(m => new MonthSpendResponse($"{m.Year:D4}-{m.Month:D2}", m.TotalAmount, m.ReceiptCount))
                .ToList();

            return Results.Ok(new SpendByMonthResponse(months.Sum(m => m.TotalAmount), months));
        });
    }

    // What counts as "spend": line-item amounts on the user's Confirmed receipts, placed by purchase date.
    // Receipts still awaiting review are left out until confirmed, and undated receipts can't be
    // placed on a timeline, so both endpoints exclude them and stay consistent with each other.
    private static IQueryable<ReceiptLineItem> SpendLineItems(ReceiptIqDbContext dbContext, Guid userId, DateOnly? from, DateOnly? to)
    {
        var items = dbContext.ReceiptLineItems.Where(li =>
            li.Receipt!.UserId == userId
            && li.Receipt.Status == ReceiptStatus.Confirmed
            && li.Receipt.PurchaseDate != null);

        if (from.HasValue)
        {
            items = items.Where(li => li.Receipt!.PurchaseDate >= from);
        }

        if (to.HasValue)
        {
            items = items.Where(li => li.Receipt!.PurchaseDate <= to);
        }

        return items;
    }
}
