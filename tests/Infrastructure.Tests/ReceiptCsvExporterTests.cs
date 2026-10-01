using System.Globalization;
using Application.Receipts;

namespace Infrastructure.Tests;

public class ReceiptCsvExporterTests
{
    [Fact]
    public void BuildCsv_WritesHeaderAndOneRowPerLineItem()
    {
        var csv = ReceiptCsvExporter.BuildCsv([
            new ReceiptExportRow(new DateOnly(2026, 9, 3), "Fake Grocery", "Milk", "Groceries", 3.99m, 24.95m, "Confirmed")
        ]);

        var lines = csv.TrimEnd('\r', '\n').Split('\n');
        Assert.Equal("Purchase Date,Merchant,Line Item,Category,Line Item Amount,Receipt Total,Status", lines[0].TrimEnd('\r'));
        Assert.Equal("2026-09-03,Fake Grocery,Milk,Groceries,3.99,24.95,Confirmed", lines[1].TrimEnd('\r'));
    }

    [Fact]
    public void BuildCsv_QuotesFieldsContainingCommasOrQuotes()
    {
        var csv = ReceiptCsvExporter.BuildCsv([
            new ReceiptExportRow(null, "Joe's \"Diner\", Inc.", "2% Milk, 1gal", null, 4.5m, null, "NeedsReview")
        ]);

        var dataLine = csv.TrimEnd('\r', '\n').Split('\n')[1].TrimEnd('\r');
        Assert.Equal(",\"Joe's \"\"Diner\"\", Inc.\",\"2% Milk, 1gal\",,4.5,,NeedsReview", dataLine);
    }

    [Fact]
    public void BuildCsv_QuotesFieldsContainingNewlines()
    {
        var csv = ReceiptCsvExporter.BuildCsv([
            new ReceiptExportRow(null, "Multi\nLine", null, null, null, null, "Confirmed")
        ]);

        Assert.Contains("\"Multi\nLine\"", csv);
    }

    [Fact]
    public void BuildCsv_UsesInvariantCultureForDecimals()
    {
        var original = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            var csv = ReceiptCsvExporter.BuildCsv([
                new ReceiptExportRow(null, null, null, null, 1234.5m, null, "Confirmed")
            ]);

            Assert.Contains("1234.5,", csv);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void BuildCsv_WritesHeaderOnlyForNoRows()
    {
        var csv = ReceiptCsvExporter.BuildCsv([]);

        Assert.Single(csv.TrimEnd('\r', '\n').Split('\n'));
    }
}
