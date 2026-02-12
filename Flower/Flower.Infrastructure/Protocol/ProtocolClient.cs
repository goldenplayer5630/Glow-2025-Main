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
        private readonly ConcurrentDictionary<int, Guid> _pendingByFlower = new(); // flower → corrId

        public ProtocolClient(ITransport transport, IFrameCodec codec)
        {
            _transport = transport;
            _codec = codec;
            _transport.FrameReceived += OnFrame;
        }

        private enum AckKind { None, Ack, Nack }

        private static AckKind TryParseAckOrNack(ReadOnlySpan<byte> raw, out int flowerId)
        {
            flowerId = 0;

            // Need at least "0/ACK" => 5
            if (raw.Length < 5) return AckKind.None;

            int i = 0;
            while (i < raw.Length && (raw[i] == (byte)' ' || raw[i] == (byte)'\t')) i++;

            // parse digits
            int id = 0, digits = 0;
            while (i < raw.Length && raw[i] >= (byte)'0' && raw[i] <= (byte)'9')
            {
                id = (id * 10) + (raw[i] - (byte)'0');
                i++; digits++;
            }

            if (digits == 0) return AckKind.None;
            if (i >= raw.Length || raw[i] != (byte)'/') return AckKind.None;
            i++; // skip '/'

            ReadOnlySpan<byte> rest = raw[i..];

            // Accept both ACK and NACK (and allow suffixes like ":stuff" or CR)
            if (rest.StartsWith("ACK"u8))
            {
                flowerId = id;
                return AckKind.Ack;
            }

            if (rest.StartsWith("NACK"u8))
            {
                flowerId = id;
                return AckKind.Nack;
            }

            return AckKind.None;
        }

        private void OnFrame(object? sender, ReadOnlyMemory<byte> raw)
        {
            var span = raw.Span;

            var kind = TryParseAckOrNack(span, out var flowerId);
            if (kind == AckKind.None)
            {
                // Not an ACK/NACK line → ignore (do NOT kill pending)
                Debug.WriteLine($"[ProtocolClient] Unknown frame: {System.Text.Encoding.ASCII.GetString(span)}");
                return;
            }

            // Only now is flowerId valid for completing a pending request
            if (_pendingByFlower.TryRemove(flowerId, out var corrId) &&
                _pending.TryRemove(corrId, out var tcs))
            {
                var env = new ProtocolEnvelope(
                    corrId,
                    flowerId,
                    CommandId: kind == AckKind.Ack ? "ack-line" : "nack-line",
                    Frames: Array.Empty<byte[]>(),
                    Type: kind == AckKind.Ack ? ProtocolMessageType.Ack : ProtocolMessageType.Nack);

                tcs.TrySetResult(env);
            }
        }

        public async Task<ProtocolEnvelope> SendAndWaitAckAsync(ProtocolEnvelope env, TimeSpan timeout, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<ProtocolEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (!_pending.TryAdd(env.CorrelationId, tcs))
                throw new InvalidOperationException($"Duplicate CorrelationId: {env.CorrelationId}");

            // NOTE: this overwrites if multiple requests are in-flight to the same flower
            _pendingByFlower[env.FlowerId] = env.CorrelationId;

            try
            {
                foreach (var frame in env.Frames)
                {
                    await _transport.WriteAsync(EnsureLf(frame), ct).ConfigureAwait(false);
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

            static byte[] EnsureLf(byte[] f)
            {
                if (f.Length > 0 && f[^1] == (byte)'\n') return f;

                // Avoid LINQ Concat allocation
                var withLf = new byte[f.Length + 1];
                Buffer.BlockCopy(f, 0, withLf, 0, f.Length);
                withLf[^1] = (byte)'\n';
                return withLf;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _transport.FrameReceived -= OnFrame;
            await _transport.DisposeAsync();
        }
    }
}
