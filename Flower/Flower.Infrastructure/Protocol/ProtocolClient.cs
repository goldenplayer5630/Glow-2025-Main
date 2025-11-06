// Flower.Infrastructure/Protocol/ProtocolClient.cs
using Flower.Core.Abstractions;
using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using Flower.Core.Records;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Flower.Infrastructure.Protocol
{
    public sealed class ProtocolClient : IProtocolClient, IAsyncDisposable
    {
        private readonly ITransport _transport;
        private readonly IFrameCodec _codec;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ProtocolEnvelope>> _pending = new();
        private readonly ConcurrentDictionary<int, Guid> _pendingByFlower = new(); // NEW: flower → corrId


        public ProtocolClient(ITransport transport, IFrameCodec codec)
        {
            _transport = transport;
            _codec = codec;
            _transport.FrameReceived += OnFrame;
        }

        private static bool TryParseAckLine(ReadOnlySpan<byte> raw, out int flowerId)
        {
            // Accept: "6/ACK", "6/ACK\r", "6/ACK\n", "6/ACK:stuff", "6/ACK:stuff\r\n"
            flowerId = 0;
            if (raw.Length < 5) return false;

            // skip leading spaces
            int i = 0;
            while (i < raw.Length && (raw[i] == (byte)' ' || raw[i] == (byte)'\t')) i++;

            // parse leading digits
            int id = 0, digits = 0;
            while (i < raw.Length && raw[i] >= (byte)'0' && raw[i] <= (byte)'9')
            {
                id = id * 10 + (raw[i] - (byte)'0');
                i++; digits++;
            }
            if (digits == 0 || i >= raw.Length || raw[i] != (byte)'/') return false;
            i++; // skip '/'

            // must start with "ACK"
            ReadOnlySpan<byte> rest = raw[i..];
            if (!rest.StartsWith("ACK"u8)) return false;

            // all good
            flowerId = id;
            return true;
        }


        private void OnFrame(object? sender, ReadOnlyMemory<byte> raw)
        {
            if (TryParseAckLine(raw.Span, out var flowerId) || flowerId == 0)
            {
                if (_pendingByFlower.TryRemove(flowerId, out var corrId) &&
                    _pending.TryRemove(corrId, out var tcs))
                {
                    // fabricate a minimal envelope to satisfy callers
                    var ackEnv = new ProtocolEnvelope(
                        corrId, flowerId, CommandId: "ack-line",
                        Frames: Array.Empty<byte[]>(),
                        Type: ProtocolMessageType.Ack);

                    tcs.TrySetResult(ackEnv);
                }
            }
            else
            {
                // parsed wrong or did not receive what was expected
                Debug.WriteLine($"[ProtocolClient] Received unknown frame: {System.Text.Encoding.ASCII.GetString(raw.Span)}");

                if (_pendingByFlower.TryRemove(flowerId, out var corrId) &&
                    _pending.TryRemove(corrId, out var tcs))
                {
                    // fabricate a minimal envelope to satisfy callers
                    var ackEnv = new ProtocolEnvelope(
                        corrId, flowerId, CommandId: "nack-line",
                        Frames: Array.Empty<byte[]>(),
                        Type: ProtocolMessageType.Nack);

                    tcs.TrySetResult(ackEnv);
                }
            }
        }

        public async Task<ProtocolEnvelope> SendAndWaitAckAsync(ProtocolEnvelope env, TimeSpan timeout, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<ProtocolEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pending.TryAdd(env.CorrelationId, tcs))
                throw new InvalidOperationException($"Duplicate CorrelationId: {env.CorrelationId}");

            _pendingByFlower[env.FlowerId] = env.CorrelationId;   // <-- map flower to corr

            try
            {
                foreach (var frame in env.Frames)
                {
                    var toSend = EnsureLf(frame);
                    await _transport.WriteAsync(toSend, ct).ConfigureAwait(false);
                }

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linked.CancelAfter(timeout);
                using (linked.Token.Register(() => tcs.TrySetCanceled(linked.Token)))
                    return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                _pending.TryRemove(env.CorrelationId, out _);
                _pendingByFlower.TryRemove(env.FlowerId, out _);
            }

            static byte[] EnsureLf(byte[] f) =>
                f.Length > 0 && f[^1] == (byte)'\n' ? f : f.Concat(new byte[] { (byte)'\n' }).ToArray();
        }

        public async ValueTask DisposeAsync()
        {
            _transport.FrameReceived -= OnFrame;
            await _transport.DisposeAsync();
        }
    }
}
