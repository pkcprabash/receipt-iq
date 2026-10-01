using Application.Budgets;
using Domain.Entities;

namespace Infrastructure.Tests;

public class BudgetAlertPolicyTests
{
    [Theory]
    [InlineData(0, 500, BudgetStatus.Ok)]
    [InlineData(399, 500, BudgetStatus.Ok)]
    [InlineData(400, 500, BudgetStatus.Warning)]
    [InlineData(450, 500, BudgetStatus.Warning)]
    [InlineData(500, 500, BudgetStatus.Exceeded)]
    [InlineData(600, 500, BudgetStatus.Exceeded)]
    public void DetermineStatus_AppliesThresholds(decimal spend, decimal limit, BudgetStatus expected)
    {
        Assert.Equal(expected, BudgetAlertPolicy.DetermineStatus(spend, limit));
    }

    [Fact]
    public void DetermineStatus_ZeroLimitWithNoSpend_IsOk()
    {
        Assert.Equal(BudgetStatus.Ok, BudgetAlertPolicy.DetermineStatus(0, 0));
    }

    [Fact]
    public void DetermineStatus_ZeroLimitWithAnySpend_IsExceeded()
    {
        Assert.Equal(BudgetStatus.Exceeded, BudgetAlertPolicy.DetermineStatus(0.01m, 0));
    }
}
