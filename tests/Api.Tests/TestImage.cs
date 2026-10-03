namespace Api.Tests;

// The upload pipeline only checks magic bytes (ReceiptImageValidator) and never decodes the
// image for real — FakeReceiptExtractor returns canned data regardless of content — so a
// minimal byte array with a valid PNG signature is all a test upload needs. Random padding
// keeps each call's SHA-256 hash unique, since the API rejects duplicate uploads per user.
public static class TestImage
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static byte[] CreatePngBytes()
    {
        var padding = new byte[32];
        Random.Shared.NextBytes(padding);
        return [.. PngSignature, .. padding];
    }
}
