using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests;

public class ReceiptProcessingWorkerTests
{
    private static IServiceScopeFactory CreateScopeFactory(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ReceiptIqDbContext>(options => options.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static ReceiptProcessingWorker CreateWorker(IServiceScopeFactory scopeFactory) =>
        new(new ReceiptProcessingQueue(), scopeFactory, NullLogger<ReceiptProcessingWorker>.Instance);

    [Fact]
    public async Task ProcessReceiptAsync_TransitionsUploadedReceiptToConfirmed()
    {
        var scopeFactory = CreateScopeFactory(Guid.NewGuid().ToString());
        Guid receiptId;

        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ReceiptIqDbContext>();
            var receipt = new Receipt
            {
                UserId = Guid.NewGuid(),
                ImageStorageKey = "key.jpg",
                ImageContentType = "image/jpeg",
                ImageHash = "hash"
            };
            dbContext.Receipts.Add(receipt);
            await dbContext.SaveChangesAsync();
            receiptId = receipt.Id;
        }

        await CreateWorker(scopeFactory).ProcessReceiptAsync(receiptId, CancellationToken.None);

        using var verifyScope = scopeFactory.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ReceiptIqDbContext>();
        var processed = await verifyContext.Receipts.FindAsync(receiptId);

        Assert.Equal(ReceiptStatus.Confirmed, processed!.Status);
    }

    [Fact]
    public async Task ProcessReceiptAsync_UnknownReceiptId_DoesNothing()
    {
        var scopeFactory = CreateScopeFactory(Guid.NewGuid().ToString());

        await CreateWorker(scopeFactory).ProcessReceiptAsync(Guid.NewGuid(), CancellationToken.None);
    }
}
