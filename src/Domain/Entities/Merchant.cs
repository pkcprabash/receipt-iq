using Domain.Common;

namespace Domain.Entities;

public class Merchant : Entity
{
    public required string Name { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
