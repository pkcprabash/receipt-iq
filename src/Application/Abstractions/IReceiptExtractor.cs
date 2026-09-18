namespace Application.Abstractions;

public interface IReceiptExtractor
{
    Task<ReceiptExtractionResult> ExtractAsync(Stream imageContent, string contentType, CancellationToken cancellationToken = default);
}

public record ReceiptExtractionLineItem(string Description, decimal? Quantity, decimal? UnitPrice, decimal TotalPrice);

public record ReceiptExtractionResult(
    string? MerchantName,
    DateOnly? PurchaseDate,
    decimal? TotalAmount,
    double Confidence,
    IReadOnlyList<ReceiptExtractionLineItem> LineItems,
    string RawResponseJson);
