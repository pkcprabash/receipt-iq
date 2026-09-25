namespace Api.Contracts.Receipts;

public record ReceiptSummaryResponse(
    Guid Id,
    DateTime UploadedAtUtc,
    DateOnly? PurchaseDate,
    decimal? TotalAmount,
    Guid? MerchantId,
    string? MerchantName,
    Guid? DominantCategoryId,
    string Status);
