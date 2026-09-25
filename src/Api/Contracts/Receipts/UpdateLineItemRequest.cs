namespace Api.Contracts.Receipts;

public record UpdateLineItemRequest(string? Description, decimal Amount);
