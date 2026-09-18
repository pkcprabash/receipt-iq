using Domain.Entities;

namespace Application.Receipts;

public static class ReceiptExtractionPolicy
{
    public const double MinConfidenceForAutoConfirm = 0.7;

    public static ReceiptStatus DetermineStatus(double confidence) =>
        confidence >= MinConfidenceForAutoConfirm ? ReceiptStatus.Confirmed : ReceiptStatus.NeedsReview;
}
