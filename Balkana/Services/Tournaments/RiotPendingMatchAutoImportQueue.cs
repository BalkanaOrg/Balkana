using System.Threading;
using System.Threading.Channels;

namespace Balkana.Services.Tournaments
{
    /// <summary>
    /// Simple in-memory queue for pending-match auto imports.
    /// </summary>
    public class RiotPendingMatchAutoImportQueue : IRiotPendingMatchAutoImportQueue
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        public ChannelReader<int> Reader => _channel.Reader;

        public ValueTask EnqueueAsync(int pendingMatchId, CancellationToken ct = default)
            => _channel.Writer.WriteAsync(pendingMatchId, ct);
    }
}

