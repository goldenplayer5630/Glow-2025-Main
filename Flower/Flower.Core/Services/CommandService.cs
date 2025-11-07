// Flower.Core/Services/CommandService.cs
using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Factories;
using Flower.Core.Enums;
using Flower.Core.Models;
using Flower.Core.Records;

namespace Flower.Core.Services
{
    public sealed class CommandService : ICommandService
    {
        private readonly ICommandRequestFactory _requestFactory;
        private readonly ICmdDispatcher _dispatcher;

        public CommandService(ICommandRequestFactory requestFactory, ICmdDispatcher dispatcher)
        {
            _requestFactory = requestFactory;
            _dispatcher = dispatcher;
        }

        public async Task<CommandOutcome> SendCommandAsync(
            string commandId,
            FlowerUnit flowerUnit,
            IReadOnlyDictionary<string, object> args,
            CancellationToken ct = default)
        {
            // Build one canonical CommandRequest via the shared rules
            var req = _requestFactory.BuildFor(
                flowerUnit,
                commandId,
                args?.ToDictionary(kv => kv.Key, kv => (object?)kv.Value) ?? null,
                ackTimeout: TimeSpan.FromMilliseconds(400) // you can override per-call here if you like
            );

            // Hand off to dispatcher
            var outcome = await _dispatcher.EnqueueAsync(req, ct).ConfigureAwait(false);
            return outcome;
        }
    }
}
