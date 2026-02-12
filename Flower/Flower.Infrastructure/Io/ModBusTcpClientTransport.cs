// Flower.Infrastructure.Io/ModbusTcpClientTransport.cs
using EasyModbus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Io
{
    public sealed class ModbusTcpClientTransport : IAsyncDisposable
    {
        private ModbusClient? _client;

        public bool IsConnected => _client?.Connected == true;

        public Task ConnectAsync(string host, int port, int connectTimeoutMs, CancellationToken ct = default)
        {
            // EasyModbus doesn't offer true async connect; keep API async-friendly.
            _client = new ModbusClient(host, port)
            {
                ConnectionTimeout = connectTimeoutMs
            };

            _client.Connect();
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            try { _client?.Disconnect(); } catch { }
            return Task.CompletedTask;
        }

        public Task WriteCoilAsync(int coilAddress, bool value, CancellationToken ct = default)
        {
            if (_client is null || !_client.Connected) throw new InvalidOperationException("Modbus not connected.");
            _client.WriteSingleCoil(coilAddress, value);
            return Task.CompletedTask;
        }

        public Task WriteRegisterAsync(int holdingRegisterAddress, int value, CancellationToken ct = default)
        {
            if (_client is null || !_client.Connected) throw new InvalidOperationException("Modbus not connected.");
            _client.WriteSingleRegister(holdingRegisterAddress, value);
            return Task.CompletedTask;
        }

        public Task<int[]> ReadHoldingRegistersAsync(int startAddress, int count, CancellationToken ct = default)
        {
            if (_client is null || !_client.Connected) throw new InvalidOperationException("Modbus not connected.");
            var res = _client.ReadHoldingRegisters(startAddress, count);
            return Task.FromResult(res);
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
        }
    }
}
