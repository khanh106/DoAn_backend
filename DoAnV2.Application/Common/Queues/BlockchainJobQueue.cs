using System.Threading.Channels;

namespace DoAnV2.Application.Common.Queues;

/// <summary>
/// Job cần xử lý blockchain bất đồng bộ.
/// </summary>
public sealed record BlockchainJob(
    Guid BatchId,
    DateTime EnqueuedAt);

/// <summary>
/// Hàng đợi in-memory cho các job blockchain.
/// Singleton lifetime — Channel&lt;T&gt; an toàn với multi-thread.
/// </summary>
public interface IBlockchainJobQueue
{
    ValueTask EnqueueAsync(Guid batchId, CancellationToken ct = default);
    IAsyncEnumerable<BlockchainJob> DequeueAllAsync(CancellationToken ct);
}

public sealed class BlockchainJobQueue : IBlockchainJobQueue
{
    private readonly Channel<BlockchainJob> _channel = Channel.CreateUnbounded<BlockchainJob>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(Guid batchId, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(new BlockchainJob(batchId, DateTime.UtcNow), ct);

    public IAsyncEnumerable<BlockchainJob> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}