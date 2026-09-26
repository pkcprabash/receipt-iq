using Domain.Common;

namespace Domain.Entities;

public class Receipt : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid? MerchantId { get; set; }
    public Merchant? Merchant { get; set; }

    public DateOnly? PurchaseDate { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public decimal? TotalAmount { get; set; }

    public required string ImageStorageKey { get; set; }
    public required string ImageContentType { get; set; }
    public long ImageSizeBytes { get; set; }
    public required string ImageHash { get; set; }

    public ReceiptStatus Status { get; set; } = ReceiptStatus.Uploaded;

    // Kept forever, even after mapping to the fields above, so extraction can be redone later.
    public string? RawOcrResponse { get; set; }

    // Only a receipt waiting on review can be confirmed; anything else is left untouched.
    public bool TryConfirm()
    {
        if (Status != ReceiptStatus.NeedsReview)
        {
            return false;
        }

        Status = ReceiptStatus.Confirmed;
        return true;
    }

    public ICollection<ReceiptLineItem> LineItems { get; set; } = new List<ReceiptLineItem>();

    // Displayed category is derived, not stored — dominant by total spend among the receipt's items.
    public Guid? DominantCategoryId =>
        LineItems
            .Where(item => item.CategoryId.HasValue)
            .GroupBy(item => item.CategoryId)
            .OrderByDescending(group => group.Sum(item => item.Amount))
            .Select(group => group.Key)
            .FirstOrDefault();
}
