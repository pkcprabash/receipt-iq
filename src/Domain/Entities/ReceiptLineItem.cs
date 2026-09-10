using Domain.Common;

namespace Domain.Entities;

public class ReceiptLineItem : Entity
{
    public Guid ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    public required string Description { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal Amount { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
}
