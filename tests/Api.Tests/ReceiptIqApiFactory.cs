using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

// Overrides just enough config to point the app at the Testcontainers Postgres instance
// and an isolated temp folder for IFileStorage — everything else (JWT signing key,
// IReceiptExtractor/ILlmCategoryClassifier choice, CORS) comes from appsettings.Development.json
// exactly as it does for a real local run, so the fake extractor and no-op classifier are
// picked up automatically (no Azure/OpenAI keys configured).
public class ReceiptIqApiFactory(string connectionString, string fileStorageRootPath) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Postgres", connectionString);
        builder.UseSetting("FileStorage:RootPath", fileStorageRootPath);
    }
}
