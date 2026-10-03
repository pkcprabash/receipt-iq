using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Api.Tests;

// One real Postgres container and one real app host, shared across every test class in the
// "Api" collection so the (slow) container only starts once per test run. Each test still
// gets isolated data by using its own randomly-generated user/email — the schema is shared,
// the rows aren't.
public class ApiTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = null!;
    private ReceiptIqApiFactory _factory = null!;
    private string _storageRoot = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("receiptiq")
            .WithUsername("receiptiq")
            .WithPassword("receiptiq")
            .Build();
        await _postgres.StartAsync();

        _storageRoot = Path.Combine(Path.GetTempPath(), $"receiptiq-tests-{Guid.NewGuid():N}");

        _factory = new ReceiptIqApiFactory(_postgres.GetConnectionString(), _storageRoot);
        Client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceiptIqDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();

        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }

    // Registers a fresh user with a unique email and returns an HttpClient already carrying
    // its bearer token — the shape every endpoint test needs to get to "logged in".
    public async Task<(HttpClient Client, string Email)> CreateAuthenticatedClientAsync()
    {
        var email = $"{Guid.NewGuid():N}@test.receiptiq.dev";
        var register = await Client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password = "Passw0rd!23",
            displayName = "Test User"
        });
        register.EnsureSuccessStatusCode();

        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return (client, email);
    }

    private record AuthResponseDto(string Token, DateTime ExpiresAtUtc);
}

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiTestFixture>;
