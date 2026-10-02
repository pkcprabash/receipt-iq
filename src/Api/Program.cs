using Api.Endpoints;
using Api.ErrorHandling;
using Infrastructure;
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

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// First in the pipeline so it catches anything thrown by middleware below it too.
app.UseExceptionHandler();

// One structured log line per request (method, path, status code, elapsed time) —
// separate from each endpoint's own logging, and enough on its own for traffic/latency questions.
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DependencyInjection.LocalFrontendDevCorsPolicy);
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

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
