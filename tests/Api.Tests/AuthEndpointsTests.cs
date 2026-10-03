using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Api.Tests;

[Collection("Api")]
public class AuthEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task Register_ThenLogin_ReturnsAToken()
    {
        var email = $"{Guid.NewGuid():N}@test.receiptiq.dev";

        var register = await fixture.Client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password = "Passw0rd!23",
            displayName = "Ada Lovelace"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await fixture.Client.PostAsJsonAsync("/auth/login", new { email, password = "Passw0rd!23" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Register_WithAlreadyRegisteredEmail_ReturnsConflict()
    {
        var email = $"{Guid.NewGuid():N}@test.receiptiq.dev";
        var request = new { email, password = "Passw0rd!23", displayName = "Dup User" };

        var first = await fixture.Client.PostAsJsonAsync("/auth/register", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await fixture.Client.PostAsJsonAsync("/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid():N}@test.receiptiq.dev";
        await fixture.Client.PostAsJsonAsync("/auth/register", new { email, password = "Passw0rd!23", displayName = "T" });

        var login = await fixture.Client.PostAsJsonAsync("/auth/login", new { email, password = "WrongPassword!1" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var login = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new { email = $"{Guid.NewGuid():N}@nowhere.dev", password = "Passw0rd!23" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }
}
