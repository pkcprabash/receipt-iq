using Application.Abstractions;
using Domain.Entities;
using Infrastructure.Persistence;
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

        var receipt = await dbContext.Receipts.FindAsync([receiptId], cancellationToken);
        if (receipt is null)
        {
            return;
        }

        try
        {
            receipt.Status = ReceiptStatus.Processing;
            await dbContext.SaveChangesAsync(cancellationToken);

            // Real extraction lands in Day 11 (fake) / Day 12 (Azure); for now processing always succeeds.
            receipt.Status = ReceiptStatus.Confirmed;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to process receipt {ReceiptId}", receiptId);
            receipt.Status = ReceiptStatus.Failed;
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }
}
