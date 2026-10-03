using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Api.Tests;

[Collection("Api")]
public class ReceiptsEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task Upload_ProcessesThroughToConfirmed_WithLineItemsAndMerchant()
    {
        var (client, _) = await fixture.CreateAuthenticatedClientAsync();

        var upload = await client.UploadReceiptAsync();
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var uploaded = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var receiptId = uploaded.GetProperty("id").GetGuid();

        // Exercises the real pipeline end to end: upload -> background worker -> FakeReceiptExtractor
        // -> merchant resolution -> category rule engine -> Confirmed (its canned confidence is 0.95).
        var receipt = await client.WaitForTerminalStatusAsync(receiptId);

        Assert.Equal("Confirmed", receipt.Status);
        Assert.NotNull(receipt.MerchantName);
        Assert.NotEmpty(receipt.LineItems);
        Assert.All(receipt.LineItems, li => Assert.True(li.Amount > 0));
        Assert.NotNull(receipt.TotalAmount);
    }

    [Fact]
    public async Task Upload_SameImageTwice_SecondIsRejectedAsDuplicate()
    {
        var (client, _) = await fixture.CreateAuthenticatedClientAsync();
        var imageBytes = TestImage.CreatePngBytes();

        var first = await client.UploadReceiptAsync(imageBytes);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.UploadReceiptAsync(imageBytes);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Upload_WithDisallowedContentType_ReturnsBadRequest()
    {
        var (client, _) = await fixture.CreateAuthenticatedClientAsync();

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent("not an image"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "notes.txt");

        var response = await client.PostAsync("/receipts/", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await fixture.Client.UploadReceiptAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsTheCallingUsersOwnReceipts()
    {
        var (clientA, _) = await fixture.CreateAuthenticatedClientAsync();
        var (clientB, _) = await fixture.CreateAuthenticatedClientAsync();

        await clientA.UploadReceiptAsync();
        await clientA.UploadReceiptAsync();
        await clientB.UploadReceiptAsync();

        var listA = await (await clientA.GetAsync("/receipts/")).Content.ReadFromJsonAsync<JsonElement>();
        var listB = await (await clientB.GetAsync("/receipts/")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(2, listA.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, listB.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Confirm_OnAnAlreadyConfirmedReceipt_ReturnsConflict()
    {
        var (client, _) = await fixture.CreateAuthenticatedClientAsync();
        var upload = await client.UploadReceiptAsync();
        var uploaded = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var receiptId = uploaded.GetProperty("id").GetGuid();
        await client.WaitForTerminalStatusAsync(receiptId); // FakeReceiptExtractor always lands on Confirmed.

        var confirm = await client.PostAsync($"/receipts/{receiptId}/confirm", content: null);

        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
    }

    [Fact]
    public async Task GetById_ForAnotherUsersReceipt_ReturnsNotFound()
    {
        var (owner, _) = await fixture.CreateAuthenticatedClientAsync();
        var (otherUser, _) = await fixture.CreateAuthenticatedClientAsync();
        var upload = await owner.UploadReceiptAsync();
        var uploaded = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var receiptId = uploaded.GetProperty("id").GetGuid();

        var response = await otherUser.GetAsync($"/receipts/{receiptId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_ReturnsCsvWithHeaderAndUploadedReceiptAsARow()
    {
        var (client, _) = await fixture.CreateAuthenticatedClientAsync();
        var upload = await client.UploadReceiptAsync();
        var uploaded = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var receiptId = uploaded.GetProperty("id").GetGuid();
        await client.WaitForTerminalStatusAsync(receiptId);

        var response = await client.GetAsync("/receipts/export");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csv = await response.Content.ReadAsStringAsync();
        var lines = csv.TrimEnd('\r', '\n').Split('\n');
        Assert.StartsWith("Purchase Date,Merchant,Line Item,Category,Line Item Amount,Receipt Total,Status", lines[0].TrimStart('﻿'));
        Assert.True(lines.Length > 1);
    }

    [Fact]
    public async Task Upload_PastTheRateLimit_Returns429WithRetryAfter()
    {
        var (client, _) = await fixture.CreateAuthenticatedClientAsync();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 21; i++)
        {
            last = await client.UploadReceiptAsync();
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.True(last.Headers.RetryAfter is not null);
    }
}
