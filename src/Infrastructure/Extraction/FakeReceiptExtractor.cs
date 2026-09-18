using System.Text.Json;
using Application.Abstractions;

namespace Infrastructure.Extraction;

// Canned data so the upload-to-processing pipeline works end-to-end before
// Azure Document Intelligence is wired in (used when it isn't configured).
public class FakeReceiptExtractor : IReceiptExtractor
{
    public Task<ReceiptExtractionResult> ExtractAsync(Stream imageContent, string contentType, CancellationToken cancellationToken = default)
    {
        var lineItems = new List<ReceiptExtractionLineItem>
        {
            new("Milk", 1m, 3.99m, 3.99m),
            new("Bread", 2m, 2.49m, 4.98m),
            new("Eggs", 1m, 5.99m, 5.99m),
            new("Paper Towels", 1m, 9.99m, 9.99m)
        };

        const string merchantName = "Fake Grocery Co.";
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalAmount = lineItems.Sum(item => item.TotalPrice);
        const double confidence = 0.95;

        var rawResponseJson = JsonSerializer.Serialize(new
        {
            merchantName,
            purchaseDate,
            totalAmount,
            confidence,
            lineItems
        });

        var result = new ReceiptExtractionResult(merchantName, purchaseDate, totalAmount, confidence, lineItems, rawResponseJson);
        return Task.FromResult(result);
    }
}
