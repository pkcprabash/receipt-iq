using Infrastructure.Processing;

namespace Infrastructure.Tests;

public class ReceiptProcessingQueueTests
{
    [Fact]
    public async Task QueueAsync_ThenReadAllAsync_YieldsQueuedId()
    {
        var queue = new ReceiptProcessingQueue();
        var receiptId = Guid.NewGuid();

        await queue.QueueAsync(receiptId);

        using var cts = new CancellationTokenSource();
        await using var enumerator = queue.ReadAllAsync(cts.Token).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(receiptId, enumerator.Current);
    }
}
