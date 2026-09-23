using System.Text;
using Application.Abstractions;
using Infrastructure.Auth;
using Infrastructure.Categorization;
using Infrastructure.Extraction;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Processing;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

public static class DependencyInjection
{
    public const string LocalFrontendDevCorsPolicy = "LocalFrontendDev";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres configuration.");

        services.AddDbContext<ReceiptIqDbContext>(options => options.UseNpgsql(connectionString));

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres");

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ReceiptIqDbContext>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing Jwt configuration.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true
                };
            });

        services.AddAuthorization();

        // Applied only in Development (see Program.cs) — production CORS is Day 48's job,
        // scoped to the deployed frontend's real origin.
        services.AddCors(options =>
        {
            options.AddPolicy(LocalFrontendDevCorsPolicy, policy =>
                policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod());
        });

        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalDiskFileStorage>();

        services.AddSingleton<ReceiptProcessingQueue>();
        services.AddSingleton<IReceiptProcessingQueue>(sp => sp.GetRequiredService<ReceiptProcessingQueue>());
        services.AddHostedService<ReceiptProcessingWorker>();

        services.Configure<DocumentIntelligenceOptions>(configuration.GetSection(DocumentIntelligenceOptions.SectionName));
        var documentIntelligenceOptions = configuration.GetSection(DocumentIntelligenceOptions.SectionName).Get<DocumentIntelligenceOptions>();
        if (!string.IsNullOrWhiteSpace(documentIntelligenceOptions?.Endpoint) && !string.IsNullOrWhiteSpace(documentIntelligenceOptions.ApiKey))
        {
            services.AddSingleton<IReceiptExtractor, AzureDocumentIntelligenceReceiptExtractor>();
        }
        else
        {
            services.AddSingleton<IReceiptExtractor, FakeReceiptExtractor>();
        }

        services.Configure<OpenAiClassifierOptions>(configuration.GetSection(OpenAiClassifierOptions.SectionName));
        var openAiOptions = configuration.GetSection(OpenAiClassifierOptions.SectionName).Get<OpenAiClassifierOptions>();
        if (!string.IsNullOrWhiteSpace(openAiOptions?.ApiKey))
        {
            services.AddSingleton<ILlmCategoryClassifier, OpenAiCategoryClassifier>();
        }
        else
        {
            services.AddSingleton<ILlmCategoryClassifier, NoOpCategoryClassifier>();
        }

        return services;
    }
}
