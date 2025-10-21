using Flower.Core.Abstractions;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Flower.Core.Models.Services
{
    public sealed class SerialPortService : ISerialPort
    {
        private readonly SerialPortStream _port = new();
        private readonly Channel<byte[]> _tx =
            Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        private Task? _writerLoop;

        public bool IsOpen => _port.IsOpen;
        public string? PortName => _port.PortName;

        public async Task OpenAsync(string portName, int baud)
        {
            if (_port.IsOpen) _port.Close();
            _port.PortName = portName;
            _port.BaudRate = baud;
            _port.Parity = Parity.None;
            _port.DataBits = 8;
            _port.StopBits = StopBits.One;
            _port.Handshake = Handshake.None;
            _port.ReadTimeout = 250;
            _port.WriteTimeout = 250;
            _port.Open();
            _writerLoop = Task.Run(WriterAsync);
            await Task.CompletedTask;
        }

        public async Task EnqueueAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
            => await _tx.Writer.WriteAsync(data.ToArray());

        private async Task WriterAsync()
        {
            try
            {
                while (await _tx.Reader.WaitToReadAsync())
                    while (_tx.Reader.TryRead(out var buf))
                    {
                        await _port.WriteAsync(buf, 0, buf.Length);
                        await _port.FlushAsync();
                    }
            }
            catch { /* TODO: log */ }
        }

        public async Task CloseAsync()
        {
            _tx.Writer.TryComplete();
            if (_writerLoop is not null) await _writerLoop;
            _port.Close();
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
            _port.Dispose();
        }
    }
}
