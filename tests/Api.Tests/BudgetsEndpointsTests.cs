using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Api.Tests;

[Collection("Api")]
public class BudgetsEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task CreateBudget_ThenConfirmedSpend_ReflectsInCurrentSpendAndStatus()
    {
        var (client, _) = await fixture.CreateAuthenticatedClientAsync();

        // An overall (null-category) budget low enough that one receipt's total pushes it past
        // the Warning threshold — ties Day 31 (spend aggregation) to Day 39 (budget alerts).
        var createBudget = await client.PostAsJsonAsync("/budgets/", new { categoryId = (Guid?)null, monthlyLimit = 10m });
        Assert.Equal(HttpStatusCode.Created, createBudget.StatusCode);

        var upload = await client.UploadReceiptAsync();
        var uploaded = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var receiptId = uploaded.GetProperty("id").GetGuid();
        var receipt = await client.WaitForTerminalStatusAsync(receiptId);
        Assert.Equal("Confirmed", receipt.Status); // Budgets only count Confirmed, dated receipts.

        var budgets = await (await client.GetAsync("/budgets/")).Content.ReadFromJsonAsync<JsonElement>();
        var budget = budgets.EnumerateArray().Single();

        Assert.Equal((double)receipt.TotalAmount!.Value, budget.GetProperty("currentSpend").GetDouble(), precision: 2);
        Assert.Equal("Exceeded", budget.GetProperty("status").GetString()); // The receipt total is well over the $10 limit.
    }

    [Fact]
    public async Task CreateBudget_DuplicateForSameCategory_ReturnsConflict()
    {
        var (client, _) = await fixture.CreateAuthenticatedClientAsync();
        var request = new { categoryId = (Guid?)null, monthlyLimit = 100m };

        var first = await client.PostAsJsonAsync("/budgets/", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/budgets/", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateBudget_WithNonPositiveLimit_ReturnsBadRequest()
    {
        var (client, _) = await fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/budgets/", new { categoryId = (Guid?)null, monthlyLimit = 0m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
