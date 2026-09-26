namespace Application.Analytics;

public record MonthTotal(int Year, int Month, decimal TotalAmount, int ReceiptCount);

// Trend charts need a point for every month, not just the ones with spending —
// otherwise a quiet month silently disappears from the axis.
public static class MonthSeries
{
    public const int MaxMonths = 240;

    public static int CountMonths(DateOnly from, DateOnly to) =>
        (to.Year - from.Year) * 12 + (to.Month - from.Month) + 1;

    // Returns every month from `from`'s month through `to`'s month, oldest first,
    // using zeroes where `totals` has no entry. Totals outside the range are dropped.
    public static IReadOnlyList<MonthTotal> FillGaps(IEnumerable<MonthTotal> totals, DateOnly from, DateOnly to)
    {
        var byMonth = totals.ToDictionary(t => (t.Year, t.Month));
        var months = CountMonths(from, to);
        if (months < 1)
        {
            return [];
        }

        var result = new List<MonthTotal>(months);
        var cursor = new DateOnly(from.Year, from.Month, 1);
        for (var i = 0; i < months; i++)
        {
            result.Add(byMonth.TryGetValue((cursor.Year, cursor.Month), out var total)
                ? total
                : new MonthTotal(cursor.Year, cursor.Month, 0m, 0));
            cursor = cursor.AddMonths(1);
        }

        return result;
    }
}
