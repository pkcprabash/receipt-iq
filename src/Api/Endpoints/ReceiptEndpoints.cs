using System.Security.Claims;
using Api.Contracts.Common;
using Api.Contracts.Receipts;
using Application.Abstractions;
using Application.Categorization;
using Application.Receipts;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Endpoints;

public static class ReceiptEndpoints
{
    private const int MaxLineItemDescriptionLength = 200;
    private const decimal MaxLineItemAmount = 10_000_000_000_000m;

    public static void MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/receipts").RequireAuthorization();

        group.MapPost("/", async (
            IFormFile file,
            ClaimsPrincipal user,
            IFileStorage fileStorage,
            ReceiptIqDbContext dbContext,
            IReceiptProcessingQueue processingQueue,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            if (file.Length == 0)
            {
                return Results.BadRequest(new { message = "File is empty." });
            }

            if (file.Length > ReceiptImageValidator.MaxSizeBytes)
            {
                return Results.BadRequest(new
                {
                    message = $"File exceeds the {ReceiptImageValidator.MaxSizeBytes / (1024 * 1024)} MB limit."
                });
            }

            if (!ReceiptImageValidator.IsAllowedContentType(file.ContentType))
            {
                return Results.BadRequest(new { message = "Unsupported content type. Allowed: JPEG, PNG, PDF." });
            }

            using var buffer = new MemoryStream();
            await using (var uploadStream = file.OpenReadStream())
            {
                await uploadStream.CopyToAsync(buffer, cancellationToken);
            }
            var bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);

            if (!ReceiptImageValidator.MatchesMagicBytes(file.ContentType, bytes))
            {
                return Results.BadRequest(new { message = "File content does not match its declared type." });
            }

            var imageHash = ImageHasher.ComputeSha256Hex(bytes);

            var duplicate = await dbContext.Receipts
                .Where(r => r.UserId == userId && r.ImageHash == imageHash)
                .Select(r => new { r.Id })
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicate is not null)
            {
                return Results.Conflict(new { message = "This receipt image has already been uploaded.", receiptId = duplicate.Id });
            }

            buffer.Position = 0;
            var storageKey = await fileStorage.SaveAsync(buffer, ReceiptImageValidator.GetExtension(file.ContentType), cancellationToken);

            var receipt = new Receipt
            {
                UserId = userId,
                UploadedAtUtc = DateTime.UtcNow,
                ImageStorageKey = storageKey,
                ImageContentType = file.ContentType,
                ImageSizeBytes = file.Length,
                ImageHash = imageHash
            };

            dbContext.Receipts.Add(receipt);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Results.Conflict(new { message = "This receipt image has already been uploaded." });
            }

            await processingQueue.QueueAsync(receipt.Id, cancellationToken);

            return Results.Created(
                $"/receipts/{receipt.Id}",
                new ReceiptUploadResponse(receipt.Id, receipt.UploadedAtUtc, receipt.Status.ToString()));
        }).DisableAntiforgery().RequireRateLimiting("receipt-upload");

        group.MapGet("/", async (
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken,
            DateOnly? from = null,
            DateOnly? to = null,
            Guid? categoryId = null,
            Guid? merchantId = null,
            string? status = null,
            int page = 1,
            int pageSize = 20) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            if (from.HasValue && to.HasValue && from > to)
            {
                return Results.BadRequest(new { message = "'from' must not be after 'to'." });
            }

            if (!TryParseStatusFilter(status, out var statusFilter, out var statusError))
            {
                return statusError;
            }

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var filtered = ApplyFilters(dbContext.Receipts.Where(r => r.UserId == userId), from, to, merchantId, categoryId, statusFilter);

            var totalCount = await filtered.CountAsync(cancellationToken);

            var receipts = await filtered
                .OrderByDescending(r => r.UploadedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(r => r.Merchant)
                .Include(r => r.LineItems)
                .ToListAsync(cancellationToken);

            var items = receipts
                .Select(r => new ReceiptSummaryResponse(
                    r.Id, r.UploadedAtUtc, r.PurchaseDate, r.TotalAmount, r.MerchantId, r.Merchant?.Name, r.DominantCategoryId, r.Status.ToString()))
                .ToList();

            return Results.Ok(new PagedResponse<ReceiptSummaryResponse>(items, page, pageSize, totalCount));
        });

        group.MapGet("/export", async (
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken,
            DateOnly? from = null,
            DateOnly? to = null,
            Guid? categoryId = null,
            Guid? merchantId = null,
            string? status = null) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            if (from.HasValue && to.HasValue && from > to)
            {
                return Results.BadRequest(new { message = "'from' must not be after 'to'." });
            }

            if (!TryParseStatusFilter(status, out var statusFilter, out var statusError))
            {
                return statusError;
            }

            var filtered = ApplyFilters(dbContext.Receipts.Where(r => r.UserId == userId), from, to, merchantId, categoryId, statusFilter);

            var receipts = await filtered
                .OrderBy(r => r.PurchaseDate)
                .ThenBy(r => r.UploadedAtUtc)
                .Include(r => r.Merchant)
                .Include(r => r.LineItems).ThenInclude(li => li.Category)
                .ToListAsync(cancellationToken);

            // One row per line item — a receipt's "category" is derived and ambiguous,
            // but each line item has exactly one, so that's the natural export grain.
            var rows = receipts.SelectMany(r => r.LineItems.DefaultIfEmpty(), (r, li) => new ReceiptExportRow(
                r.PurchaseDate,
                r.Merchant?.Name,
                li?.Description,
                li?.CategoryId is null ? null : (li.Category?.Name ?? "Uncategorized"),
                li?.Amount,
                r.TotalAmount,
                r.Status.ToString()));

            var csv = ReceiptCsvExporter.BuildCsv(rows);
            var fileName = $"receipts-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

            // UTF-8 BOM so Excel (which guesses encoding without it) renders this correctly.
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray();
            return Results.File(bytes, "text/csv", fileName);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            var receipt = await dbContext.Receipts
                .Include(r => r.Merchant)
                .Include(r => r.LineItems)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

            if (receipt is null)
            {
                return Results.NotFound();
            }

            var response = new ReceiptDetailResponse(
                receipt.Id,
                receipt.UploadedAtUtc,
                receipt.PurchaseDate,
                receipt.TotalAmount,
                receipt.MerchantId,
                receipt.Merchant?.Name,
                receipt.ImageContentType,
                receipt.ImageSizeBytes,
                receipt.DominantCategoryId,
                receipt.Status.ToString(),
                receipt.LineItems
                    .Select(li => new ReceiptLineItemResponse(li.Id, li.Description, li.Amount, li.CategoryId))
                    .ToList());

            return Results.Ok(response);
        });

        group.MapPost("/{id:guid}/confirm", async (
            Guid id,
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            var receipt = await dbContext.Receipts
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);
            if (receipt is null)
            {
                return Results.NotFound();
            }

            if (!receipt.TryConfirm())
            {
                return Results.Conflict(new { message = "Only receipts that need review can be confirmed." });
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });

        group.MapGet("/{id:guid}/image", async (
            Guid id,
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            IFileStorage fileStorage,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            var receipt = await dbContext.Receipts
                .Where(r => r.Id == id && r.UserId == userId)
                .Select(r => new { r.ImageStorageKey, r.ImageContentType })
                .FirstOrDefaultAsync(cancellationToken);
            if (receipt is null)
            {
                return Results.NotFound();
            }

            Stream stream;
            try
            {
                stream = await fileStorage.OpenReadAsync(receipt.ImageStorageKey, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }

            // Content-Type comes from the magic-byte-validated upload, but stop browsers from second-guessing it.
            httpContext.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.Stream(stream, receipt.ImageContentType);
        });

        group.MapPut("/{receiptId:guid}/line-items/{lineItemId:guid}", async (
            Guid receiptId,
            Guid lineItemId,
            UpdateLineItemRequest request,
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            var description = request.Description?.Trim();
            if (string.IsNullOrEmpty(description))
            {
                return Results.BadRequest(new { message = "Description is required." });
            }

            if (description.Length > MaxLineItemDescriptionLength)
            {
                return Results.BadRequest(new { message = $"Description must be at most {MaxLineItemDescriptionLength} characters." });
            }

            // Negative amounts are legitimate (discounts, coupons); the bound just keeps it inside decimal(18,2).
            if (Math.Abs(request.Amount) >= MaxLineItemAmount)
            {
                return Results.BadRequest(new { message = "Amount is out of range." });
            }

            var lineItem = await dbContext.ReceiptLineItems
                .FirstOrDefaultAsync(li => li.Id == lineItemId && li.ReceiptId == receiptId && li.Receipt!.UserId == userId, cancellationToken);
            if (lineItem is null)
            {
                return Results.NotFound();
            }

            lineItem.Description = description;
            lineItem.Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new ReceiptLineItemResponse(lineItem.Id, lineItem.Description, lineItem.Amount, lineItem.CategoryId));
        });

        group.MapPut("/{receiptId:guid}/line-items/{lineItemId:guid}/category", async (
            Guid receiptId,
            Guid lineItemId,
            UpdateLineItemCategoryRequest request,
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            var receipt = await dbContext.Receipts
                .FirstOrDefaultAsync(r => r.Id == receiptId && r.UserId == userId, cancellationToken);
            if (receipt is null)
            {
                return Results.NotFound();
            }

            var lineItem = await dbContext.ReceiptLineItems
                .FirstOrDefaultAsync(li => li.Id == lineItemId && li.ReceiptId == receiptId, cancellationToken);
            if (lineItem is null)
            {
                return Results.NotFound();
            }

            var categoryExists = await dbContext.Categories
                .AnyAsync(c => c.Id == request.CategoryId && (c.UserId == null || c.UserId == userId), cancellationToken);
            if (!categoryExists)
            {
                return Results.BadRequest(new { message = "Unknown category." });
            }

            lineItem.CategoryId = request.CategoryId;
            await UpsertUserCategoryRuleAsync(dbContext, userId, receipt.MerchantId, lineItem.Description, request.CategoryId, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new ReceiptLineItemResponse(lineItem.Id, lineItem.Description, lineItem.Amount, lineItem.CategoryId));
        });

        group.MapPost("/recategorize", async (
            ClaimsPrincipal user,
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            var rules = await dbContext.CategoryRules
                .Where(r => r.UserId == null || r.UserId == userId)
                .ToListAsync(cancellationToken);

            var lineItems = await dbContext.ReceiptLineItems
                .Where(li => li.Receipt!.UserId == userId)
                .Select(li => new { LineItem = li, MerchantId = li.Receipt!.MerchantId })
                .ToListAsync(cancellationToken);

            var updatedCount = 0;
            foreach (var entry in lineItems)
            {
                var matchedCategoryId = CategoryRuleEngine.TryMatch(rules, entry.MerchantId, entry.LineItem.Description);
                if (matchedCategoryId is { } categoryId && entry.LineItem.CategoryId != categoryId)
                {
                    entry.LineItem.CategoryId = categoryId;
                    updatedCount++;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new RecategorizeResponse(updatedCount));
        });
    }

    private static async Task UpsertUserCategoryRuleAsync(
        ReceiptIqDbContext dbContext,
        Guid userId,
        Guid? merchantId,
        string lineItemDescription,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var existingRule = merchantId.HasValue
            ? await dbContext.CategoryRules
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MerchantId == merchantId && r.Keyword == null, cancellationToken)
            : await dbContext.CategoryRules
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Keyword == lineItemDescription && r.MerchantId == null, cancellationToken);

        if (existingRule is not null)
        {
            existingRule.CategoryId = categoryId;
            return;
        }

        dbContext.CategoryRules.Add(new CategoryRule
        {
            CategoryId = categoryId,
            UserId = userId,
            MerchantId = merchantId,
            Keyword = merchantId.HasValue ? null : lineItemDescription
        });
    }

    private static bool TryParseStatusFilter(string? status, out ReceiptStatus? statusFilter, out IResult errorResult)
    {
        statusFilter = null;
        errorResult = null!;

        if (string.IsNullOrEmpty(status))
        {
            return true;
        }

        if (!Enum.TryParse<ReceiptStatus>(status, ignoreCase: true, out var parsedStatus) || !Enum.IsDefined(parsedStatus))
        {
            errorResult = Results.BadRequest(new { message = "Unknown status." });
            return false;
        }

        statusFilter = parsedStatus;
        return true;
    }

    private static IQueryable<Receipt> ApplyFilters(
        IQueryable<Receipt> query, DateOnly? from, DateOnly? to, Guid? merchantId, Guid? categoryId, ReceiptStatus? status)
    {
        // Date filters apply to the purchase date, so receipts without one drop out when either is set.
        if (from.HasValue)
        {
            query = query.Where(r => r.PurchaseDate >= from);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.PurchaseDate <= to);
        }

        if (merchantId.HasValue)
        {
            query = query.Where(r => r.MerchantId == merchantId);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status);
        }

        // Categories live on line items, so this matches receipts containing the category at all,
        // not just those where it is the dominant one.
        if (categoryId.HasValue)
        {
            query = query.Where(r => r.LineItems.Any(li => li.CategoryId == categoryId));
        }

        return query;
    }
}
