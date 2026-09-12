namespace Application.Abstractions;

public interface IFileStorage
{
    // Storage decides the key (never trust a caller-supplied file name for a path).
    Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
