namespace Api.Contracts.Budgets;

public record BudgetRequest(Guid? CategoryId, decimal MonthlyLimit);
