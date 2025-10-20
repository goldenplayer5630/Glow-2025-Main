using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions
{
    public interface ISerialPort : IAsyncDisposable
    {
        /// <summary>True if the port is currently open.</summary>
        bool IsOpen { get; }

        /// <summary>The underlying port name (e.g., COM3, /dev/ttyUSB0), or null if closed.</summary>
        string? PortName { get; }

        /// <summary>Open the serial port with the given name and baud rate.</summary>
        Task OpenAsync(string portName, int baud);

        /// <summary>Close the serial port (no-op if already closed).</summary>
        Task CloseAsync();

        /// <summary>Queue bytes to be written to the port. Implementation should be single-writer safe.</summary>
        Task EnqueueAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default);
    }
}