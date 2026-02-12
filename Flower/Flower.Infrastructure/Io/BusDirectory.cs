using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using Flower.Core.Models;
using Flower.Infrastructure.Protocol;
using System.Collections.Concurrent;

namespace Flower.Infrastructure.Io
{
    public sealed class BusDirectory : IBusDirectory
    {
        private readonly IFrameCodec _codec;

        // Modbus needs a mapper; inject it (or a factory) here
        private readonly IModBusCommandMapper _modbusMapper;

        // busId -> entry
        private readonly ConcurrentDictionary<string, BusEntry> _buses = new();
        private readonly SemaphoreSlim _gate = new(1, 1);

        private sealed class BusEntry
        {
            public required BusConfig Config { get; init; }
            public required IBusClient Client { get; init; }
        }

        public BusDirectory(IFrameCodec codec, IModBusCommandMapper modbusMapper)
        {
            _codec = codec;
            _modbusMapper = modbusMapper;
        }

        public async Task OpenAsync(IEnumerable<BusConfig> configs)
        {
            if (configs is null) return;
            foreach (var cfg in configs)
                await ConnectAsync(cfg).ConfigureAwait(false);
        }

        public async Task ConnectAsync(BusConfig cfg)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            if (string.IsNullOrWhiteSpace(cfg.BusId)) throw new ArgumentException("BusId required", nameof(cfg));

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_buses.TryRemove(cfg.BusId, out var old))
                    await SafeDisposeAsync(old).ConfigureAwait(false);

                var entry = cfg.BusType switch
                {
                    BusType.SerialBus => await CreateSerialAsync(cfg).ConfigureAwait(false),
                    BusType.ModbusTcp => await CreateModbusAsync(cfg).ConfigureAwait(false),
                    _ => throw new ArgumentOutOfRangeException(nameof(cfg.BusType), cfg.BusType, "Unsupported bus type")
                };

                _buses[cfg.BusId] = entry;
            }
            catch
            {
                _buses.TryRemove(cfg.BusId, out var half);
                if (half is not null) await SafeDisposeAsync(half).ConfigureAwait(false);
                throw;
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<BusEntry> CreateSerialAsync(BusConfig cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg.Port)) throw new ArgumentException("Port required", nameof(cfg));
            if (cfg.Baud <= 0) throw new ArgumentException("Baud must be > 0", nameof(cfg));

            var transport = new SerialPortTransport();
            await transport.OpenAsync(cfg.Port, cfg.Baud).ConfigureAwait(false);

            var protocol = new ProtocolClient(transport, _codec);
            var client = new SerialBusClient(transport, protocol);

            return new BusEntry { Config = cfg, Client = client };
        }

        private async Task<BusEntry> CreateModbusAsync(BusConfig cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg.ModbusHost)) throw new ArgumentException("ModbusHost required", nameof(cfg));
            if (cfg.ModbusPort <= 0 || cfg.ModbusPort > 65535) throw new ArgumentException("ModbusPort invalid", nameof(cfg));

            var transport = new ModbusTcpClientTransport();
            await transport.ConnectAsync(cfg.ModbusHost, cfg.ModbusPort, cfg.ModbusConnectTimeoutMs).ConfigureAwait(false);

            var client = new ModbusBusClient(transport, _modbusMapper);

            return new BusEntry { Config = cfg, Client = client };
        }

        public async Task DisconnectAsync(string busId)
        {
            if (string.IsNullOrWhiteSpace(busId)) return;

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_buses.TryRemove(busId, out var entry))
                    await SafeDisposeAsync(entry).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public bool IsOpen(string busId)
        {
            if (string.IsNullOrWhiteSpace(busId)) return false;
            return _buses.TryGetValue(busId, out var e) && e.Client.IsOpen;
        }

        public IBusClient GetClient(string busId)
        {
            if (string.IsNullOrWhiteSpace(busId))
                throw new ArgumentNullException(nameof(busId));

            if (_buses.TryGetValue(busId, out var e))
                return e.Client;

            throw new KeyNotFoundException($"Bus '{busId}' is not connected.");
        }

        public async ValueTask DisposeAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var entries = _buses.Values.ToArray();
                _buses.Clear();

                foreach (var e in entries)
                    await SafeDisposeAsync(e).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private static async Task SafeDisposeAsync(BusEntry e)
        {
            try { await e.Client.DisposeAsync(); } catch { }
        }
    }
}
