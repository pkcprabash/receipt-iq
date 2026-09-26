using Application.Analytics;

namespace Infrastructure.Tests;

public class MonthSeriesTests
{
    [Fact]
    public void FillGaps_InsertsZeroMonthsBetweenData()
    {
        var totals = new[]
        {
            new MonthTotal(2026, 1, 100m, 2),
            new MonthTotal(2026, 4, 40m, 1)
        };

        var result = MonthSeries.FillGaps(totals, new DateOnly(2026, 1, 15), new DateOnly(2026, 4, 2));

        Assert.Equal([(2026, 1, 100m), (2026, 2, 0m), (2026, 3, 0m), (2026, 4, 40m)],
            result.Select(m => (m.Year, m.Month, m.TotalAmount)));
        Assert.Equal(0, result[1].ReceiptCount);
    }

    [Fact]
    public void FillGaps_CrossesYearBoundaryInOrder()
    {
        var result = MonthSeries.FillGaps([], new DateOnly(2025, 11, 1), new DateOnly(2026, 2, 28));

        Assert.Equal([(2025, 11), (2025, 12), (2026, 1), (2026, 2)], result.Select(m => (m.Year, m.Month)));
    }

    [Fact]
    public void FillGaps_DropsTotalsOutsideTheRange()
    {
        var totals = new[] { new MonthTotal(2025, 12, 9m, 1), new MonthTotal(2026, 1, 5m, 1) };

        var result = MonthSeries.FillGaps(totals, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Single(result);
        Assert.Equal(5m, result[0].TotalAmount);
    }

    [Fact]
    public void CountMonths_IsInclusiveOfBothEnds()
    {
        Assert.Equal(1, MonthSeries.CountMonths(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));
        Assert.Equal(13, MonthSeries.CountMonths(new DateOnly(2025, 1, 31), new DateOnly(2026, 1, 1)));
    }
}
