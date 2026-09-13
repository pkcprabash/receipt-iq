using System.Security.Claims;
using Api.Contracts.Receipts;
using Application.Abstractions;
using Application.Receipts;
using Domain.Entities;
using Infrastructure.Persistence;

namespace Api.Endpoints;

public static class ReceiptEndpoints
{
    public static void MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/receipts").RequireAuthorization();

        group.MapPost("/", async (
            IFormFile file,
            ClaimsPrincipal user,
            IFileStorage fileStorage,
            ReceiptIqDbContext dbContext) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Results.Unauthorized();
            }

            if (file.Length == 0)
            {
                return Results.BadRequest(new { message = "File is empty." });
            }

            if (file.Length > ReceiptImageValidator.MaxSizeBytes)
            {
                return Results.BadRequest(new
                {
                    message = $"File exceeds the {ReceiptImageValidator.MaxSizeBytes / (1024 * 1024)} MB limit."
                });
            }

            if (!ReceiptImageValidator.IsAllowedContentType(file.ContentType))
            {
                return Results.BadRequest(new { message = "Unsupported content type. Allowed: JPEG, PNG, PDF." });
            }

            var header = new byte[8];
            int bytesRead;
            await using (var probeStream = file.OpenReadStream())
            {
                bytesRead = await probeStream.ReadAsync(header);
            }

            if (!ReceiptImageValidator.MatchesMagicBytes(file.ContentType, header.AsSpan(0, bytesRead)))
            {
                return Results.BadRequest(new { message = "File content does not match its declared type." });
            }

            await using var uploadStream = file.OpenReadStream();
            var storageKey = await fileStorage.SaveAsync(uploadStream, ReceiptImageValidator.GetExtension(file.ContentType));

            var receipt = new Receipt
            {
                UserId = userId,
                UploadedAtUtc = DateTime.UtcNow,
                ImageStorageKey = storageKey,
                ImageContentType = file.ContentType,
                ImageSizeBytes = file.Length
            };

            dbContext.Receipts.Add(receipt);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/receipts/{receipt.Id}", new ReceiptUploadResponse(receipt.Id, receipt.UploadedAtUtc));
        }).DisableAntiforgery();
    }
}
