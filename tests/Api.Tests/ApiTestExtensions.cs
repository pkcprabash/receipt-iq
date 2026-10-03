using System.Net.Http.Json;

namespace Api.Tests;

public static class ApiTestExtensions
{
    public static async Task<HttpResponseMessage> UploadReceiptAsync(this HttpClient client, byte[]? imageBytes = null)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(imageBytes ?? TestImage.CreatePngBytes());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "receipt.png");
        return await client.PostAsync("/receipts/", content);
    }

    // The fake extractor runs on a background worker, same as production — poll instead of
    // assuming it's done the instant the upload call returns, exactly as the frontend does.
    public static async Task<ReceiptDetailDto> WaitForTerminalStatusAsync(this HttpClient client, Guid receiptId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/receipts/{receiptId}");
            response.EnsureSuccessStatusCode();
            var receipt = await response.Content.ReadFromJsonAsync<ReceiptDetailDto>();

            if (receipt!.Status is "Confirmed" or "NeedsReview" or "Failed")
            {
                return receipt;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Receipt {receiptId} did not reach a terminal status within the timeout.");
    }
}

public record ReceiptLineItemDto(Guid Id, string Description, decimal Amount, Guid? CategoryId);

public record ReceiptDetailDto(
    Guid Id,
    DateTime UploadedAtUtc,
    DateOnly? PurchaseDate,
    decimal? TotalAmount,
    Guid? MerchantId,
    string? MerchantName,
    string ImageContentType,
    long ImageSizeBytes,
    Guid? DominantCategoryId,
    string Status,
    List<ReceiptLineItemDto> LineItems);
