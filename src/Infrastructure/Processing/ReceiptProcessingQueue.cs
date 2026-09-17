using System.Threading.Channels;
using Application.Abstractions;

namespace Infrastructure.Processing;

public class ReceiptProcessingQueue : IReceiptProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public ValueTask QueueAsync(Guid receiptId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(receiptId, cancellationToken);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
