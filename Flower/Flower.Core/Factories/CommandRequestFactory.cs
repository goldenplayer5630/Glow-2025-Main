// Flower.Core/Services/CommandRequestFactory.cs
using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Factories;
using Flower.Core.Enums;
using Flower.Core.Models;
using Flower.Core.Records;

namespace Flower.Core.Services
{
    public sealed class CommandRequestFactory : ICommandRequestFactory
    {
        private readonly ICommandRegistry _commands;

        public CommandRequestFactory(ICommandRegistry commands)
        {
            _commands = commands;
        }

        public CommandRequest BuildFor(
            FlowerUnit flower,
            string commandId,
            IReadOnlyDictionary<string, object?>? rawArgs,
            TimeSpan? ackTimeout = null)
        {
            if (flower is null) throw new ArgumentNullException(nameof(flower));
            if (string.IsNullOrWhiteSpace(commandId)) throw new ArgumentNullException(nameof(commandId));

            // Start with originals; we may rewrite below.
            var cmdId = commandId;
            var args = new Dictionary<string, object?>(rawArgs ?? new Dictionary<string, object?>(), StringComparer.OrdinalIgnoreCase);

            // Resolve current command
            var cmd = _commands.GetById(cmdId);

            // Validate support
            if (!cmd.SupportedCategories.Contains(flower.Category))
                throw new InvalidOperationException($"Command {cmd.Id} not supported for {flower.Category}.");

            // ---- Idempotency + smart rewrite ----
            // If OPEN/CLOSE is a no-op, we’ll skip it via ShouldSkip.
            // If a combined motor+led.ramp is a no-op for the motor, rewrite to plain led.ramp (keep endIntensity/durationMs).
            Func<FlowerUnit, bool>? shouldSkip = cmd.Id switch
            {
                "motor.open" => f => f.FlowerStatus == FlowerStatus.Open,
                "motor.close" => f => f.FlowerStatus == FlowerStatus.Closed,
                _ => null
            };

            bool isNoOpNow = shouldSkip?.Invoke(flower) ?? false;

            if (isNoOpNow && (cmd.Id == "motor.open.led.ramp" || cmd.Id == "motor.close.led.ramp"))
            {
                cmdId = "led.ramp";
                args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["endIntensity"] = args.TryGetValue("endIntensity", out var ei) ? ei : 0,
                    ["durationMs"] = args.TryGetValue("durationMs", out var dm) ? dm : 1500
                };
            }

            if (cmdId != commandId)
                cmd = _commands.GetById(cmdId);

            // Args validation for (possibly) rewritten command
            cmd.ValidateArgs(flower.Category, args);

            // Frames + expected state transitions
            var frames = cmd.BuildPayload(flower.Id, flower.Category, args);
            var onAck = cmd.StateOnAck(flower.Category, args);
            var onTimeout = static (FlowerUnit f) =>
            {
                f.ConnectionStatus = ConnectionStatus.Degraded;
                return f;
            };

            var timeout = ackTimeout ?? TimeSpan.FromMilliseconds(400);

            return new CommandRequest(
                BusId: flower.BusId,
                FlowerId: flower.Id,
                CommandId: cmd.Id,
                Args: args,
                AckTimeout: timeout,
                StateOnAck: onAck,
                StateOnTimeout: onTimeout,
                Frames: frames,
                ShouldSkip: shouldSkip
            );
        }
    }
}
