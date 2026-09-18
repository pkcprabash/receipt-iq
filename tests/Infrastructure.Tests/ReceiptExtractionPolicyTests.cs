using Application.Receipts;
using Domain.Entities;

namespace Infrastructure.Tests;

public class ReceiptExtractionPolicyTests
{
    [Theory]
    [InlineData(0.95, ReceiptStatus.Confirmed)]
    [InlineData(0.7, ReceiptStatus.Confirmed)]
    [InlineData(0.69, ReceiptStatus.NeedsReview)]
    [InlineData(0.0, ReceiptStatus.NeedsReview)]
    public void DetermineStatus_BranchesOnConfidenceThreshold(double confidence, ReceiptStatus expected)
    {
        Assert.Equal(expected, ReceiptExtractionPolicy.DetermineStatus(confidence));
    }
}
