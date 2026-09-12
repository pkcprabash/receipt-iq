using System.Text;
using Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests;

public class LocalDiskFileStorageTests : IDisposable
{
    private readonly string _rootPath;
    private readonly LocalDiskFileStorage _storage;

    public LocalDiskFileStorageTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "receiptiq-tests", Guid.NewGuid().ToString("N"));
        _storage = new LocalDiskFileStorage(Options.Create(new FileStorageOptions { RootPath = _rootPath }));
    }

    [Fact]
    public async Task SaveAsync_ThenOpenReadAsync_RoundTripsContent()
    {
        var original = "receipt bytes"u8.ToArray();
        var storageKey = await _storage.SaveAsync(new MemoryStream(original), ".jpg");

        await using var readStream = await _storage.OpenReadAsync(storageKey);
        using var reader = new MemoryStream();
        await readStream.CopyToAsync(reader);

        Assert.Equal(original, reader.ToArray());
        Assert.EndsWith(".jpg", storageKey);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheSavedFile()
    {
        var storageKey = await _storage.SaveAsync(new MemoryStream("data"u8.ToArray()), ".png");

        await _storage.DeleteAsync(storageKey);

        Assert.False(File.Exists(Path.Combine(_rootPath, storageKey)));
    }

    [Fact]
    public async Task OpenReadAsync_RejectsStorageKeyThatEscapesRootPath()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.OpenReadAsync("../outside.jpg"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
