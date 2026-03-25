using System.Threading;
using System.Threading.Channels;

namespace Balkana.Services.Tournaments
{
    public interface IRiotPendingMatchAutoImportQueue
    {
        ChannelReader<int> Reader { get; }
        ValueTask EnqueueAsync(int pendingMatchId, CancellationToken ct = default);
    }
}

