using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Commands
{
    public interface ITransport : IAsyncDisposable
    {
        bool IsOpen { get; }
        string? PortName { get; }
        Task OpenAsync(string portName, int baud);
        Task CloseAsync();

        // Already-framed bytes
        Task WriteAsync(ReadOnlyMemory<byte> frame, CancellationToken ct = default);

        // Fired by read loop with raw bytes of one complete frame (or pass parsed envelope if you prefer)
        event EventHandler<ReadOnlyMemory<byte>>? FrameReceived;
    }

}
