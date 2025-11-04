using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using Flower.Core.Models;
using Flower.Core.Records;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Flower.Core.Services
{
    public sealed class CommandService : ICommandService
    {
        private readonly ICommandRegistry _commands;
        private readonly ICmdDispatcher _dispatcher;
        private CancellationTokenSource? _cts;

        public CommandService(
            ICommandRegistry commands,
            ICmdDispatcher dispatcher)
        {
            _commands = commands;
            _dispatcher = dispatcher;
        }

        public async Task SendCommandAsync(
            string commandId,
            FlowerUnit flowerUnit,
            IReadOnlyDictionary<string, object> args,
            CancellationToken ct = default)
        {
            var cmd = _commands.GetById(commandId);

            IReadOnlyDictionary<string, object?> safeArgs = args != null
                ? args.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)
                : new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());

            var frame = cmd.BuildPayload(
                flowerUnit.Id,
                flowerUnit.Category,
                safeArgs
            );

            var onAck = cmd.StateOnAck(flowerUnit.Category, safeArgs); // e.g., set Open / brightness, mark Healthy
            var onTimeout = static (FlowerUnit f) =>
            {
                f.ConnectionStatus = ConnectionStatus.Degraded;
                return f;
            };

            var cmdRequest = new CommandRequest(
                BusId: flowerUnit.BusId,
                CommandId: cmd.Id,
                FlowerId: flowerUnit.Id,
                Args: safeArgs,
                AckTimeout: TimeSpan.FromMilliseconds(400),
                StateOnAck: onAck,
                StateOnTimeout: onTimeout
            );

            await _dispatcher.EnqueueAsync(
                cmdRequest,
                ct
            ).ConfigureAwait(false);
        }
    }
}