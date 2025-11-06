// Flower.Infrastructure.Io/SerialPortTransport.cs
using Flower.Core.Abstractions.Commands;
using RJCP.IO.Ports;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Io
{
    /// <summary>
    /// Fast line-oriented serial transport.
    /// - Writes: appends '\n' if missing.
    /// - Reads: bulk ReadAsync + scan for '\n', trims optional '\r', raises FrameReceived per line.
    /// </summary>
    public sealed class SerialPortTransport : ITransport
    {
        private readonly SerialPortStream _port = new();

        private CancellationTokenSource? _cts;
        private Task? _readerLoop;

        public bool IsOpen => _port.IsOpen;
        public string? PortName => _port.PortName;

        public event EventHandler<ReadOnlyMemory<byte>>? FrameReceived;

        public async Task OpenAsync(string portName, int baud)
        {
            if (_port.IsOpen) await CloseAsync();

            _port.PortName = portName;
            _port.BaudRate = baud;
            _port.Parity = Parity.None;
            _port.DataBits = 8;
            _port.StopBits = StopBits.One;
            _port.Handshake = Handshake.None;

            // Keep these short; we rely on async reads (no per-byte timeouts).
            _port.ReadTimeout = 5;
            _port.WriteTimeout = 50;

            // Give the driver some headroom to burst ACKs quickly
            _port.ReadBufferSize = Math.Max(_port.ReadBufferSize, 64 * 1024);
            _port.WriteBufferSize = Math.Max(_port.WriteBufferSize, 16 * 1024);

            _port.Open();

            _cts = new CancellationTokenSource();
            _readerLoop = Task.Run(() => ReaderAsync(_cts.Token));
        }

        public Task WriteAsync(ReadOnlyMemory<byte> frame, CancellationToken ct = default)
        {
            var span = frame.Span;

            // Ensure single LF terminator (your caller may already send CRLF)
            if (span.Length == 0 || span[^1] != (byte)'\n')
            {
                var rented = ArrayPool<byte>.Shared.Rent(span.Length + 1);
                try
                {
                    span.CopyTo(rented);
                    rented[span.Length] = (byte)'\n';
                    _port.Write(rented, 0, span.Length + 1);
                }
                finally { ArrayPool<byte>.Shared.Return(rented); }
            }
            else
            {
                // already newline-terminated
                _port.Write(span);
            }

            // RJCP stream writes are synchronous; flush to nudge the OS buffer
            _port.Flush();
            return Task.CompletedTask;
        }

        private async Task ReaderAsync(CancellationToken ct)
        {
            // Rolling buffer + scan for '\n'
            byte[] buf = ArrayPool<byte>.Shared.Rent(4096);
            int used = 0;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Expand if nearly full
                    if (used >= buf.Length - 512)
                    {
                        var bigger = ArrayPool<byte>.Shared.Rent(buf.Length * 2);
                        Buffer.BlockCopy(buf, 0, bigger, 0, used);
                        ArrayPool<byte>.Shared.Return(buf);
                        buf = bigger;
                    }

                    int nRead;
                    try
                    {
                        // ReadAsync returns as soon as some bytes arrive or timeout happens
                        nRead = await _port.ReadAsync(buf, used, buf.Length - used, ct)
                                           .ConfigureAwait(false);
                    }
                    catch (TimeoutException)
                    {
                        // harmless; loop continues
                        continue;
                    }

                    if (nRead <= 0) continue;
                    used += nRead;

                    // Scan for LF within [0..used)
                    int searchStart = 0;
                    for (int i = 0; i < used; i++)
                    {
                        if (buf[i] == (byte)'\n')
                        {
                            int lineEndExclusive = i; // excludes LF
                            int lineLen = lineEndExclusive - searchStart;

                            if (lineLen > 0)
                            {
                                // Trim trailing CR if present
                                if (buf[searchStart + lineLen - 1] == (byte)'\r') lineLen--;

                                if (lineLen > 0)
                                {
                                    // Raise the slice
                                    var line = new byte[lineLen];
                                    Buffer.BlockCopy(buf, searchStart, line, 0, lineLen);
                                    FrameReceived?.Invoke(this, line);
                                }
                            }

                            searchStart = i + 1; // next fragment starts after LF
                        }
                    }

                    // Compact leftover (partial) data to start of buffer
                    int remaining = used - searchStart;
                    if (remaining > 0)
                        Buffer.BlockCopy(buf, searchStart, buf, 0, remaining);
                    used = remaining;
                }
            }
            catch (OperationCanceledException) { /* closing */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Serial RX error: {ex}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        public async Task CloseAsync()
        {
            try { _cts?.Cancel(); } catch { }
            if (_readerLoop is not null)
            {
                try { await _readerLoop.ConfigureAwait(false); } catch { }
                _readerLoop = null;
            }
            try { _port.Close(); } catch { }
            _cts?.Dispose(); _cts = null;
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
            _port.Dispose();
        }
    }
}
