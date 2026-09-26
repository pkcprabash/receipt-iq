using Domain.Common;

namespace Domain.Entities;

// A null UserId marks a system category shared by everyone; a set UserId is that user's custom category.
public class Category : Entity
{
    public required string Name { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsSystem => UserId is null;
}
