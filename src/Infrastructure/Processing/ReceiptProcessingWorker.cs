using Application.Abstractions;
using Application.Receipts;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

            foreach (var item in extraction.LineItems)
            {
                dbContext.ReceiptLineItems.Add(new ReceiptLineItem
                {
                    ReceiptId = receipt.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Amount = item.TotalPrice
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
        var trimmedName = merchantName.Trim();

        var existingId = await dbContext.Merchants
            .Where(m => m.Name == trimmedName)
            .Select(m => (Guid?)m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId is { } id)
        {
            return id;
        }

        var merchant = new Merchant { Name = trimmedName };
        dbContext.Merchants.Add(merchant);
        await dbContext.SaveChangesAsync(cancellationToken);
        return merchant.Id;
    }
}
