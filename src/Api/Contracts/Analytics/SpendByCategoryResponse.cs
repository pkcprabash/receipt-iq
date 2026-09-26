namespace Api.Contracts.Analytics;

public record CategorySpendResponse(Guid CategoryId, string CategoryName, decimal TotalAmount, int LineItemCount);

public record SpendByCategoryResponse(decimal TotalAmount, IReadOnlyList<CategorySpendResponse> Categories);
