using Application.Abstractions;
using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests;

public class ReceiptProcessingWorkerTests
{
    private sealed class StubFileStorage : IFileStorage
    {
        public Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken cancellationToken = default) =>
            Task.FromResult("stub-key");

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubReceiptExtractor(ReceiptExtractionResult result) : IReceiptExtractor
    {
        public Task<ReceiptExtractionResult> ExtractAsync(Stream imageContent, string contentType, CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }

    private static IServiceScopeFactory CreateScopeFactory(string dbName, ReceiptExtractionResult extractionResult)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ReceiptIqDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddSingleton<IFileStorage>(new StubFileStorage());
        services.AddSingleton<IReceiptExtractor>(new StubReceiptExtractor(extractionResult));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static ReceiptProcessingWorker CreateWorker(IServiceScopeFactory scopeFactory) =>
        new(new ReceiptProcessingQueue(), scopeFactory, NullLogger<ReceiptProcessingWorker>.Instance);

    private static async Task<Guid> SeedUploadedReceiptAsync(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
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
        return receipt.Id;
    }

    [Fact]
    public async Task ProcessReceiptAsync_HighConfidence_MapsExtractionAndConfirms()
    {
        var extraction = new ReceiptExtractionResult(
            "Fake Grocery Co.",
            new DateOnly(2026, 9, 1),
            24.95m,
            0.95,
            [new ReceiptExtractionLineItem("Milk", 1m, 3.99m, 3.99m)],
            "{\"raw\":true}");

        var scopeFactory = CreateScopeFactory(Guid.NewGuid().ToString(), extraction);
        var receiptId = await SeedUploadedReceiptAsync(scopeFactory);

        await CreateWorker(scopeFactory).ProcessReceiptAsync(receiptId, CancellationToken.None);

        using var verifyScope = scopeFactory.CreateScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<ReceiptIqDbContext>();
        var processed = await dbContext.Receipts.Include(r => r.LineItems).Include(r => r.Merchant)
            .FirstAsync(r => r.Id == receiptId);

        Assert.Equal(ReceiptStatus.Confirmed, processed.Status);
        Assert.Equal("Fake Grocery Co.", processed.Merchant?.Name);
        Assert.Equal(new DateOnly(2026, 9, 1), processed.PurchaseDate);
        Assert.Equal(24.95m, processed.TotalAmount);
        Assert.Equal("{\"raw\":true}", processed.RawOcrResponse);
        Assert.Single(processed.LineItems);
        Assert.Equal("Milk", processed.LineItems.First().Description);
    }

    [Fact]
    public async Task ProcessReceiptAsync_LowConfidence_RoutesToNeedsReview()
    {
        var extraction = new ReceiptExtractionResult(
            "Blurry Store",
            null,
            null,
            0.3,
            [],
            "{}");

        var scopeFactory = CreateScopeFactory(Guid.NewGuid().ToString(), extraction);
        var receiptId = await SeedUploadedReceiptAsync(scopeFactory);

        await CreateWorker(scopeFactory).ProcessReceiptAsync(receiptId, CancellationToken.None);

        using var verifyScope = scopeFactory.CreateScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<ReceiptIqDbContext>();
        var processed = await dbContext.Receipts.FindAsync(receiptId);

        Assert.Equal(ReceiptStatus.NeedsReview, processed!.Status);
    }

    [Fact]
    public async Task ProcessReceiptAsync_UnknownReceiptId_DoesNothing()
    {
        var extraction = new ReceiptExtractionResult(null, null, null, 1d, [], "{}");
        var scopeFactory = CreateScopeFactory(Guid.NewGuid().ToString(), extraction);

        await CreateWorker(scopeFactory).ProcessReceiptAsync(Guid.NewGuid(), CancellationToken.None);
    }
}
