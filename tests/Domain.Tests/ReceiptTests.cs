using Domain.Entities;

namespace Domain.Tests;

public class ReceiptTests
{
    [Fact]
    public void DominantCategoryId_WithNoLineItems_IsNull()
    {
        var receipt = new Receipt { UserId = Guid.NewGuid(), ImageStorageKey = "key.jpg", ImageContentType = "image/jpeg", ImageHash = "hash" };

        Assert.Null(receipt.DominantCategoryId);
    }

    [Fact]
    public void DominantCategoryId_IgnoresUncategorizedLineItems()
    {
        var receipt = new Receipt { UserId = Guid.NewGuid(), ImageStorageKey = "key.jpg", ImageContentType = "image/jpeg", ImageHash = "hash" };
        receipt.LineItems.Add(new ReceiptLineItem { Description = "Mystery item", Amount = 50m, CategoryId = null });

        Assert.Null(receipt.DominantCategoryId);
    }

    [Fact]
    public void DominantCategoryId_ReturnsCategoryWithHighestTotalSpend()
    {
        var groceries = Guid.NewGuid();
        var household = Guid.NewGuid();
        var receipt = new Receipt { UserId = Guid.NewGuid(), ImageStorageKey = "key.jpg", ImageContentType = "image/jpeg", ImageHash = "hash" };

        receipt.LineItems.Add(new ReceiptLineItem { Description = "Milk", Amount = 4.50m, CategoryId = groceries });
        receipt.LineItems.Add(new ReceiptLineItem { Description = "Bread", Amount = 3.00m, CategoryId = groceries });
        receipt.LineItems.Add(new ReceiptLineItem { Description = "Paper towels", Amount = 6.00m, CategoryId = household });

        Assert.Equal(groceries, receipt.DominantCategoryId);
    }

    [Fact]
    public void TryConfirm_FromNeedsReview_MarksConfirmed()
    {
        var receipt = new Receipt { UserId = Guid.NewGuid(), ImageStorageKey = "key.jpg", ImageContentType = "image/jpeg", ImageHash = "hash", Status = ReceiptStatus.NeedsReview };

        Assert.True(receipt.TryConfirm());
        Assert.Equal(ReceiptStatus.Confirmed, receipt.Status);
    }

    [Theory]
    [InlineData(ReceiptStatus.Uploaded)]
    [InlineData(ReceiptStatus.Processing)]
    [InlineData(ReceiptStatus.Confirmed)]
    [InlineData(ReceiptStatus.Failed)]
    public void TryConfirm_FromAnyOtherStatus_LeavesStatusUnchanged(ReceiptStatus status)
    {
        var receipt = new Receipt { UserId = Guid.NewGuid(), ImageStorageKey = "key.jpg", ImageContentType = "image/jpeg", ImageHash = "hash", Status = status };

        Assert.False(receipt.TryConfirm());
        Assert.Equal(status, receipt.Status);
    }
}
