using System.ClientModel.Primitives;
using Application.Abstractions;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Options;

namespace Infrastructure.Extraction;

public class AzureDocumentIntelligenceReceiptExtractor : IReceiptExtractor
{
    private const string ReceiptModelId = "prebuilt-receipt";

    private readonly DocumentIntelligenceClient _client;

    public AzureDocumentIntelligenceReceiptExtractor(IOptions<DocumentIntelligenceOptions> options)
    {
        var value = options.Value;
        _client = new DocumentIntelligenceClient(new Uri(value.Endpoint!), new AzureKeyCredential(value.ApiKey!));
    }

    public async Task<ReceiptExtractionResult> ExtractAsync(Stream imageContent, string contentType, CancellationToken cancellationToken = default)
    {
        var content = await BinaryData.FromStreamAsync(imageContent, cancellationToken);

        var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, ReceiptModelId, content, cancellationToken);
        var analyzeResult = operation.Value;
        var rawResponseJson = ModelReaderWriter.Write(analyzeResult, ModelReaderWriterOptions.Json).ToString();

        var document = analyzeResult.Documents.FirstOrDefault();
        if (document is null)
        {
            return new ReceiptExtractionResult(null, null, null, 0d, [], rawResponseJson);
        }

        var fields = document.Fields;

        return new ReceiptExtractionResult(
            TryGetString(fields, "MerchantName"),
            TryGetDate(fields, "TransactionDate"),
            TryGetAmount(fields, "Total"),
            document.Confidence,
            ExtractLineItems(fields),
            rawResponseJson);
    }

    private static string? TryGetString(DocumentFieldDictionary fields, string name) =>
        fields.TryGetValue(name, out var field) ? field.ValueString : null;

    private static DateOnly? TryGetDate(DocumentFieldDictionary fields, string name) =>
        fields.TryGetValue(name, out var field) && field.ValueDate is { } date
            ? DateOnly.FromDateTime(date.Date)
            : null;

    private static decimal? TryGetAmount(DocumentFieldDictionary fields, string name)
    {
        if (!fields.TryGetValue(name, out var field))
        {
            return null;
        }

        if (field.ValueCurrency is { } currency)
        {
            return (decimal)currency.Amount;
        }

        return field.ValueDouble.HasValue ? (decimal)field.ValueDouble.Value : null;
    }

    private static List<ReceiptExtractionLineItem> ExtractLineItems(DocumentFieldDictionary fields)
    {
        var items = new List<ReceiptExtractionLineItem>();

        if (!fields.TryGetValue("Items", out var itemsField) || itemsField.ValueList is null)
        {
            return items;
        }

        foreach (var itemField in itemsField.ValueList)
        {
            var itemFields = itemField.ValueDictionary;
            if (itemFields is null)
            {
                continue;
            }

            var description = TryGetString(itemFields, "Description") ?? "Unknown item";
            var quantity = itemFields.TryGetValue("Quantity", out var quantityField) && quantityField.ValueDouble.HasValue
                ? (decimal?)quantityField.ValueDouble.Value
                : null;
            var unitPrice = TryGetAmount(itemFields, "Price");
            var totalPrice = TryGetAmount(itemFields, "TotalPrice") ?? 0m;

            items.Add(new ReceiptExtractionLineItem(description, quantity, unitPrice, totalPrice));
        }

        return items;
    }
}
