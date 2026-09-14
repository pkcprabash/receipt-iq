using System.Security.Cryptography;

namespace Application.Receipts;

public static class ImageHasher
{
    public static string ComputeSha256Hex(ReadOnlySpan<byte> content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}
