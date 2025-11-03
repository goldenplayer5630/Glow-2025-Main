using Flower.Core.Abstractions.Commands;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Io
{
    public sealed class SerialPortTransport : ITransport
    {
        private readonly SerialPortStream _port = new();
        private readonly Channel<byte[]> _tx =
            Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        private Task? _writerLoop;
        private Task? _readerLoop;
        private CancellationTokenSource? _cts;

        public bool IsOpen => _port.IsOpen;
        public string? PortName => _port.PortName;

        public event EventHandler<ReadOnlyMemory<byte>>? FrameReceived;

        public async Task OpenAsync(string portName, int baud)
        {
            if (_port.IsOpen) _port.Close();

            _port.PortName = portName;
            _port.BaudRate = baud;
            _port.Parity = Parity.None;
            _port.DataBits = 8;
            _port.StopBits = StopBits.One;
            _port.Handshake = Handshake.None;
            _port.ReadTimeout = 50;    // short to keep loop responsive
            _port.WriteTimeout = 250;

            _port.Open();

            _cts = new CancellationTokenSource();
            _writerLoop = Task.Run(WriterAsync);
            _readerLoop = Task.Run(() => ReaderAsync(_cts.Token));

            await Task.CompletedTask;
        }

        public async Task WriteAsync(ReadOnlyMemory<byte> frame, CancellationToken ct = default)
            => await _tx.Writer.WriteAsync(frame.ToArray(), ct);

        private async Task WriterAsync()
        {
            try
            {
                while (await _tx.Reader.WaitToReadAsync())
                {
                    while (_tx.Reader.TryRead(out var buf))
                    {
                        await _port.WriteAsync(buf, 0, buf.Length);
                        await _port.FlushAsync();
                    }
                }
            }
            catch { /* TODO logging */ }
        }

        private async Task ReaderAsync(CancellationToken ct)
        {
            var buffer = new List<byte>(256);

            while (!ct.IsCancellationRequested && _port.IsOpen)
            {
                try
                {
                    int b = _port.ReadByte();
                    if (b < 0) { await Task.Delay(5, ct); continue; }

                    if (b == (byte)'\n') // simplistic: line-based
                    {
                        var frame = buffer.ToArray();
                        buffer.Clear();
                        FrameReceived?.Invoke(this, frame);
                    }
                    else
                    {
                        buffer.Add((byte)b);
                        if (buffer.Count > 4096) buffer.Clear(); // guard
                    }
                }
                catch (TimeoutException) { /* normal */ }
                catch (OperationCanceledException) { }
                catch
                {
                    await Task.Delay(10, ct);
                }
            }
        }

        public async Task CloseAsync()
        {
            try { _tx.Writer.TryComplete(); } catch { }
            if (_writerLoop is not null) await _writerLoop;

            try { _cts?.Cancel(); } catch { }
            if (_readerLoop is not null) await _readerLoop;

            _port.Close();
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
            _port.Dispose();
        }
    }
}
