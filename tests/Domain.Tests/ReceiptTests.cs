using Domain.Entities;

namespace Domain.Tests;

public class ReceiptTests
{
    [Fact]
    public void DominantCategoryId_WithNoLineItems_IsNull()
    {
        var receipt = new Receipt { UserId = Guid.NewGuid() };

        Assert.Null(receipt.DominantCategoryId);
    }

    [Fact]
    public void DominantCategoryId_IgnoresUncategorizedLineItems()
    {
        var receipt = new Receipt { UserId = Guid.NewGuid() };
        receipt.LineItems.Add(new ReceiptLineItem { Description = "Mystery item", Amount = 50m, CategoryId = null });

        Assert.Null(receipt.DominantCategoryId);
    }

    [Fact]
    public void DominantCategoryId_ReturnsCategoryWithHighestTotalSpend()
    {
        var groceries = Guid.NewGuid();
        var household = Guid.NewGuid();
        var receipt = new Receipt { UserId = Guid.NewGuid() };

        receipt.LineItems.Add(new ReceiptLineItem { Description = "Milk", Amount = 4.50m, CategoryId = groceries });
        receipt.LineItems.Add(new ReceiptLineItem { Description = "Bread", Amount = 3.00m, CategoryId = groceries });
        receipt.LineItems.Add(new ReceiptLineItem { Description = "Paper towels", Amount = 6.00m, CategoryId = household });

        Assert.Equal(groceries, receipt.DominantCategoryId);
    }
}
