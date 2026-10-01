namespace Application.Receipts;

// One row per line item (a receipt's own category is derived, so a line item,
// which always has exactly one, is the natural export grain). LineItem fields
// are null for a receipt that has none, so it still gets a row of its own.
public record ReceiptExportRow(
    DateOnly? PurchaseDate,
    string? MerchantName,
    string? LineItemDescription,
    string? CategoryName,
    decimal? LineItemAmount,
    decimal? ReceiptTotal,
    string Status);
