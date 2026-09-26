using Application.Abstractions;
using Application.Categorization;
using Application.Receipts;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Infrastructure.Processing;

public class ReceiptProcessingWorker(
    IReceiptProcessingQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ReceiptProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var receiptId in queue.ReadAllAsync(stoppingToken))
        {
            await ProcessReceiptAsync(receiptId, stoppingToken);
        }
    }

    public async Task ProcessReceiptAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceiptIqDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var extractor = scope.ServiceProvider.GetRequiredService<IReceiptExtractor>();
        var categoryClassifier = scope.ServiceProvider.GetRequiredService<ILlmCategoryClassifier>();

        var receipt = await dbContext.Receipts.FindAsync([receiptId], cancellationToken);
        if (receipt is null)
        {
            return;
        }

        try
        {
            receipt.Status = ReceiptStatus.Processing;
            await dbContext.SaveChangesAsync(cancellationToken);

            ReceiptExtractionResult extraction;
            await using (var imageStream = await fileStorage.OpenReadAsync(receipt.ImageStorageKey, cancellationToken))
            {
                extraction = await extractor.ExtractAsync(imageStream, receipt.ImageContentType, cancellationToken);
            }

            receipt.RawOcrResponse = extraction.RawResponseJson;
            receipt.PurchaseDate = extraction.PurchaseDate;
            receipt.TotalAmount = extraction.TotalAmount;

            if (!string.IsNullOrWhiteSpace(extraction.MerchantName))
            {
                receipt.MerchantId = await ResolveMerchantIdAsync(dbContext, extraction.MerchantName, cancellationToken);
            }

            var categoryRules = await dbContext.CategoryRules
                .Where(r => r.UserId == null || r.UserId == receipt.UserId)
                .ToListAsync(cancellationToken);
            var categoryOptions = await dbContext.Categories
                .Where(c => c.UserId == null || c.UserId == receipt.UserId)
                .Select(c => new CategoryOption(c.Id, c.Name))
                .ToListAsync(cancellationToken);

            foreach (var item in extraction.LineItems)
            {
                var categoryId = CategoryRuleEngine.TryMatch(categoryRules, receipt.MerchantId, item.Description)
                    ?? await categoryClassifier.ClassifyAsync(item.Description, extraction.MerchantName, categoryOptions, cancellationToken)
                    ?? SystemCategories.UncategorizedId;

                dbContext.ReceiptLineItems.Add(new ReceiptLineItem
                {
                    ReceiptId = receipt.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Amount = item.TotalPrice,
                    CategoryId = categoryId
                });
            }

            receipt.Status = ReceiptExtractionPolicy.DetermineStatus(extraction.Confidence);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to process receipt {ReceiptId}", receiptId);
            receipt.Status = ReceiptStatus.Failed;
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    private static async Task<Guid> ResolveMerchantIdAsync(ReceiptIqDbContext dbContext, string merchantName, CancellationToken cancellationToken)
    {
        var normalizedName = MerchantNameNormalizer.Normalize(merchantName);

        var existingId = await dbContext.Merchants
            .Where(m => m.Name == normalizedName)
            .Select(m => (Guid?)m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId is { } id)
        {
            return id;
        }

        var merchant = new Merchant { Name = normalizedName };
        dbContext.Merchants.Add(merchant);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another receipt in flight resolved the same normalized name first — use its row.
            dbContext.Merchants.Remove(merchant);
            return await dbContext.Merchants
                .Where(m => m.Name == normalizedName)
                .Select(m => m.Id)
                .FirstAsync(cancellationToken);
        }

        return merchant.Id;
    }
}
