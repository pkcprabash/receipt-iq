namespace Api.Contracts.Analytics;

public record MerchantSpendResponse(Guid? MerchantId, string MerchantName, decimal TotalAmount, int ReceiptCount);

public record SpendByMerchantResponse(decimal TotalAmount, IReadOnlyList<MerchantSpendResponse> Merchants);
