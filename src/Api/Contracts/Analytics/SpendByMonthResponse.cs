namespace Api.Contracts.Analytics;

// Month is "yyyy-MM" so the client can sort and label it without date parsing.
public record MonthSpendResponse(string Month, decimal TotalAmount, int ReceiptCount);

public record SpendByMonthResponse(decimal TotalAmount, IReadOnlyList<MonthSpendResponse> Months);
