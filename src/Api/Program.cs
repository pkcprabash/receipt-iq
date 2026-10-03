using System.Security.Claims;
using System.Threading.RateLimiting;
using Api.Endpoints;
using Api.ErrorHandling;
using Infrastructure;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog replaces the default Microsoft.Extensions.Logging providers entirely —
// "Logging" in appsettings is unused from here on; configuration lives under "Serilog".
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId());

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "ReceiptIQ API";
        document.Info.Description = "Photo-to-dashboard receipt tracking: upload, OCR extraction, categorization, and spending analytics.";
        return Task.CompletedTask;
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Keyed by user, not IP — the endpoint already requires auth, and a shared office/NAT
    // IP would otherwise throttle unrelated users together.
    options.AddPolicy("receipt-upload", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many uploads. Try again in a minute." },
            cancellationToken: cancellationToken);
    };
});

var app = builder.Build();

// First in the pipeline so it catches anything thrown by middleware below it too.
app.UseExceptionHandler();

// One structured log line per request (method, path, status code, elapsed time) —
// separate from each endpoint's own logging, and enough on its own for traffic/latency questions.
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseCors(DependencyInjection.LocalFrontendDevCorsPolicy);
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// After authorization so the partition-key selector above can read the authenticated user.
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapReceiptEndpoints();
app.MapCategoryEndpoints();
app.MapMerchantEndpoints();
app.MapAnalyticsEndpoints();
app.MapBudgetEndpoints();

try
{
    Log.Information("Starting ReceiptIQ API");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "ReceiptIQ API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
