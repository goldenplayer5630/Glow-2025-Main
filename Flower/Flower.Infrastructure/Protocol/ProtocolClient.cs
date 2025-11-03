// Flower.Infrastructure/Protocol/ProtocolClient.cs
using Flower.Core.Abstractions;
using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using Flower.Core.Records;
using System.Collections.Concurrent;

namespace Flower.Infrastructure.Protocol
{
    public sealed class ProtocolClient : IProtocolClient, IAsyncDisposable
    {
        private readonly ITransport _transport;
        private readonly IFrameCodec _codec;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ProtocolEnvelope>> _pending = new();

        public ProtocolClient(ITransport transport, IFrameCodec codec)
        {
            _transport = transport;
            _codec = codec;
            _transport.FrameReceived += OnFrame;
        }

        private void OnFrame(object? sender, ReadOnlyMemory<byte> raw)
        {
            if (!_codec.TryDecode(raw.Span, out var env)) return;

            if (env.Type is ProtocolMessageType.Ack or ProtocolMessageType.Nack)
            {
                if (_pending.TryRemove(env.CorrelationId, out var tcs))
                {
                    // Complete with whatever came back; caller decides how to treat Nack
                    tcs.TrySetResult(env);
                }
            }

            // Optional: publish events/heartbeats to observers
        }

        public async Task<ProtocolEnvelope> SendAndWaitAckAsync(
            ProtocolEnvelope command, TimeSpan timeout, CancellationToken ct = default)
        {
            if (command.Type != ProtocolMessageType.Command)
                throw new ArgumentException("Envelope.Type must be Command", nameof(command));

            var tcs = new TaskCompletionSource<ProtocolEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pending.TryAdd(command.CorrelationId, tcs))
                throw new InvalidOperationException("Duplicate CorrelationId.");

            try
            {
                var frame = _codec.Encode(command);
                await _transport.WriteAsync(frame, ct);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeout);

                using (cts.Token.Register(() => tcs.TrySetCanceled(cts.Token)))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _pending.TryRemove(command.CorrelationId, out _);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _transport.FrameReceived -= OnFrame;
            await _transport.DisposeAsync();
        }
    }
}
