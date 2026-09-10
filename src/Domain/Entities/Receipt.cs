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

    // Kept forever, even after mapping to the fields above, so extraction can be redone later.
    public string? RawOcrResponse { get; set; }

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
