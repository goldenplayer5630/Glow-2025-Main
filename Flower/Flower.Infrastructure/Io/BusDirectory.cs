using Flower.Core.Abstractions.Commands;
using Flower.Core.Models;
using Flower.Core.Records;
using Flower.Infrastructure.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Io
{
    public sealed class BusDirectory : IBusDirectory
    {
        private readonly IFrameCodec _codec;

        // busId -> entry
        private readonly ConcurrentDictionary<string, BusEntry> _buses = new();
        private readonly SemaphoreSlim _gate = new(1, 1);

        private sealed class BusEntry
        {
            public required BusConfig Config { get; init; }
            public required ITransport Transport { get; init; }
            public required IProtocolClient Protocol { get; init; }
        }

        public BusDirectory(IFrameCodec codec) => _codec = codec;

        // ---------- Bulk open ----------
        public async Task OpenAsync(IEnumerable<BusConfig> configs)
        {
            if (configs is null) return;

            // open sequentially to avoid USB driver quirks
            foreach (var cfg in configs)
                await ConnectAsync(cfg).ConfigureAwait(false);
        }

        // ---------- Single connect / reconnect ----------
        public async Task ConnectAsync(BusConfig cfg)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            if (string.IsNullOrWhiteSpace(cfg.BusId)) throw new ArgumentException("BusId required", nameof(cfg));
            if (string.IsNullOrWhiteSpace(cfg.Port)) throw new ArgumentException("Port required", nameof(cfg));
            if (cfg.Baud <= 0) throw new ArgumentException("Baud must be > 0", nameof(cfg));

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                // If the bus already exists, tear it down first (reconnect semantics)
                if (_buses.TryRemove(cfg.BusId, out var old))
                {
                    // Dispose outside of this try? It's fine to do here; they’re independent.
                    await SafeDisposeAsync(old).ConfigureAwait(false);
                }

                // Create + open a fresh transport/protocol
                var transport = new SerialPortTransport();
                await transport.OpenAsync(cfg.Port, cfg.Baud).ConfigureAwait(false);

                var protocol = new ProtocolClient(transport, _codec);

                _buses[cfg.BusId] = new BusEntry
                {
                    Config = cfg,
                    Transport = transport,
                    Protocol = protocol
                };
            }
            catch
            {
                // On failure we must ensure the busId key isn't left half-registered
                _buses.TryRemove(cfg.BusId, out var half);
                if (half is not null) await SafeDisposeAsync(half).ConfigureAwait(false);
                throw;
            }
            finally
            {
                _gate.Release();
            }
        }

        // ---------- Single disconnect ----------
        public async Task DisconnectAsync(string busId)
        {
            if (string.IsNullOrWhiteSpace(busId)) return;

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_buses.TryRemove(busId, out var entry))
                {
                    await SafeDisposeAsync(entry).ConfigureAwait(false);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        // ---------- Status ----------
        public bool IsOpen(string busId)
        {
            if (string.IsNullOrWhiteSpace(busId)) return false;
            if (!_buses.TryGetValue(busId, out var e)) return false;
            return e.Transport.IsOpen;
        }

        // ---------- Access ----------
        public IProtocolClient GetProtocol(string busId)
        {
            if (string.IsNullOrWhiteSpace(busId))
                throw new ArgumentNullException(nameof(busId));

            if (_buses.TryGetValue(busId, out var e))
                return e.Protocol;

            throw new KeyNotFoundException($"Bus '{busId}' is not connected.");
        }

        // ---------- Dispose all ----------
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
            try { await e.Protocol.DisposeAsync(); } catch { /* swallow */ }
            try { await e.Transport.DisposeAsync(); } catch { /* swallow */ }
        }
    }
}
