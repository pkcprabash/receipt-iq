using Domain.Common;

namespace Domain.Entities;

// A null CategoryId is an "overall" budget across every category; a set CategoryId
// scopes it to just that one. Checked against the current calendar month's spend.
public class Budget : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public decimal MonthlyLimit { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
