using Infrastructure.Extraction;

namespace Infrastructure.Tests;

public class FakeReceiptExtractorTests
{
    [Fact]
    public async Task ExtractAsync_ReturnsCannedDataWithConsistentTotal()
    {
        var extractor = new FakeReceiptExtractor();

        var result = await extractor.ExtractAsync(new MemoryStream(), "image/jpeg");

        Assert.False(string.IsNullOrWhiteSpace(result.MerchantName));
        Assert.NotEmpty(result.LineItems);
        Assert.Equal(result.LineItems.Sum(item => item.TotalPrice), result.TotalAmount);
        Assert.True(result.Confidence > 0.7);
        Assert.False(string.IsNullOrWhiteSpace(result.RawResponseJson));
    }
}
