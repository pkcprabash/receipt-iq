namespace Api.Contracts.Receipts;

public record ReceiptLineItemResponse(Guid Id, string Description, decimal Amount, Guid? CategoryId);
