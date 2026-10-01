using System.Globalization;
using System.Text;

namespace Application.Receipts;

public static class ReceiptCsvExporter
{
    private static readonly string[] Header =
    [
        "Purchase Date", "Merchant", "Line Item", "Category", "Line Item Amount", "Receipt Total", "Status"
    ];

    public static string BuildCsv(IEnumerable<ReceiptExportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Header));

        foreach (var row in rows)
        {
            var fields = new[]
            {
                row.PurchaseDate?.ToString("yyyy-MM-dd") ?? "",
                row.MerchantName ?? "",
                row.LineItemDescription ?? "",
                row.CategoryName ?? "",
                row.LineItemAmount?.ToString(CultureInfo.InvariantCulture) ?? "",
                row.ReceiptTotal?.ToString(CultureInfo.InvariantCulture) ?? "",
                row.Status
            };

            builder.AppendLine(string.Join(',', fields.Select(Escape)));
        }

        return builder.ToString();
    }

    // RFC 4180: a field containing a comma, quote, or newline is wrapped in quotes,
    // with any quote inside doubled up.
    private static string Escape(string field)
    {
        if (field.IndexOfAny([',', '"', '\n', '\r']) < 0)
        {
            return field;
        }

        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}
