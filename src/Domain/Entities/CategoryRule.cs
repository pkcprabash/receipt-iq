using Domain.Common;

namespace Domain.Entities;

// Matched merchant-first, then line-item keyword, else Uncategorized.
// A null UserId marks a system-wide rule; a set UserId is a per-user override.
public class CategoryRule : Entity
{
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid? MerchantId { get; set; }
    public Merchant? Merchant { get; set; }

    public string? Keyword { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
