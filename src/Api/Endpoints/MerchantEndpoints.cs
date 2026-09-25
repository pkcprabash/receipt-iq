using System.Security.Claims;
using Api.Contracts.Merchants;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class MerchantEndpoints
{
    public static void MapMerchantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/merchants").RequireAuthorization();

        // Merchants are shared across users, so only return the ones this user has receipts for.
        group.MapGet("/", async (
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            var merchants = await dbContext.Merchants
                .Where(m => dbContext.Receipts.Any(r => r.UserId == userId && r.MerchantId == m.Id))
                .OrderBy(m => m.Name)
                .Select(m => new MerchantResponse(m.Id, m.Name))
                .ToListAsync(cancellationToken);

            return Results.Ok(merchants);
        });
    }
}
