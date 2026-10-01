using Domain.Entities;

namespace Application.Budgets;

public static class BudgetAlertPolicy
{
    public const double WarningThreshold = 0.8;

    public static BudgetStatus DetermineStatus(decimal spend, decimal monthlyLimit)
    {
        if (monthlyLimit <= 0)
        {
            return spend > 0 ? BudgetStatus.Exceeded : BudgetStatus.Ok;
        }

        var ratio = (double)(spend / monthlyLimit);
        if (ratio >= 1.0)
        {
            return BudgetStatus.Exceeded;
        }

        return ratio >= WarningThreshold ? BudgetStatus.Warning : BudgetStatus.Ok;
    }
}
