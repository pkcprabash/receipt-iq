namespace Application.Abstractions;

public interface IReceiptProcessingQueue
{
    ValueTask QueueAsync(Guid receiptId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}
