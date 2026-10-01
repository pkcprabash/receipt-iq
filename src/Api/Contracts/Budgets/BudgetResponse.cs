namespace Api.Contracts.Budgets;

public record BudgetResponse(
    Guid Id,
    Guid? CategoryId,
    string CategoryName,
    decimal MonthlyLimit,
    decimal CurrentSpend,
    double PercentUsed,
    string Status);
