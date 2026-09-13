namespace Application.Receipts;

// Trusts neither the declared Content-Type nor the file name — the magic
// bytes are what decide what a file actually is.
public static class ReceiptImageValidator
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private sealed record Signature(byte[] MagicBytes, string Extension);

    private static readonly IReadOnlyDictionary<string, Signature> SignaturesByContentType = new Dictionary<string, Signature>
    {
        ["image/jpeg"] = new(new byte[] { 0xFF, 0xD8, 0xFF }, ".jpg"),
        ["image/png"] = new(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, ".png"),
        ["application/pdf"] = new("%PDF"u8.ToArray(), ".pdf")
    };

    public static bool IsAllowedContentType(string contentType) =>
        SignaturesByContentType.ContainsKey(contentType);

    public static bool MatchesMagicBytes(string contentType, ReadOnlySpan<byte> header)
    {
        if (!SignaturesByContentType.TryGetValue(contentType, out var signature))
        {
            return false;
        }

        return header.Length >= signature.MagicBytes.Length
            && header[..signature.MagicBytes.Length].SequenceEqual(signature.MagicBytes);
    }

    public static string GetExtension(string contentType) => SignaturesByContentType[contentType].Extension;
}
