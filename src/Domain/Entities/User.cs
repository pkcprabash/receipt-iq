using Domain.Common;

namespace Domain.Entities;

public class User : Entity
{
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
