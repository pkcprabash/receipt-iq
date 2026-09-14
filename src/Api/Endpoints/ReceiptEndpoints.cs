using System.Security.Claims;
using Api.Contracts.Receipts;
using Application.Abstractions;
using Application.Receipts;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            ReceiptIqDbContext dbContext,
            CancellationToken cancellationToken) =>
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

            using var buffer = new MemoryStream();
            await using (var uploadStream = file.OpenReadStream())
            {
                await uploadStream.CopyToAsync(buffer, cancellationToken);
            }
            var bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);

            if (!ReceiptImageValidator.MatchesMagicBytes(file.ContentType, bytes))
            {
                return Results.BadRequest(new { message = "File content does not match its declared type." });
            }

            var imageHash = ImageHasher.ComputeSha256Hex(bytes);

            var duplicate = await dbContext.Receipts
                .Where(r => r.UserId == userId && r.ImageHash == imageHash)
                .Select(r => new { r.Id })
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicate is not null)
            {
                return Results.Conflict(new { message = "This receipt image has already been uploaded.", receiptId = duplicate.Id });
            }

            buffer.Position = 0;
            var storageKey = await fileStorage.SaveAsync(buffer, ReceiptImageValidator.GetExtension(file.ContentType), cancellationToken);

            var receipt = new Receipt
            {
                UserId = userId,
                UploadedAtUtc = DateTime.UtcNow,
                ImageStorageKey = storageKey,
                ImageContentType = file.ContentType,
                ImageSizeBytes = file.Length,
                ImageHash = imageHash
            };

            dbContext.Receipts.Add(receipt);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Results.Conflict(new { message = "This receipt image has already been uploaded." });
            }

            return Results.Created($"/receipts/{receipt.Id}", new ReceiptUploadResponse(receipt.Id, receipt.UploadedAtUtc));
        }).DisableAntiforgery();
    }
}
