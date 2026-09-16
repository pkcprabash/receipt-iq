namespace Api.Contracts.Receipts;

public record ReceiptDetailResponse(
    Guid Id,
    DateTime UploadedAtUtc,
    DateOnly? PurchaseDate,
    decimal? TotalAmount,
    Guid? MerchantId,
    string ImageContentType,
    long ImageSizeBytes,
    Guid? DominantCategoryId,
    IReadOnlyList<ReceiptLineItemResponse> LineItems);
