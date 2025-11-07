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

            var cmdId = commandId.ToLower();
            var args = new Dictionary<string, object?>(rawArgs ?? new Dictionary<string, object?>(), StringComparer.OrdinalIgnoreCase);

            var cmd = _commands.GetById(cmdId);

            if (!cmd.SupportedCategories.Contains(flower.Category))
                throw new InvalidOperationException($"Command {cmd.Id} not supported for {flower.Category}.");

            // ---- Idempotency + smart rewrite ----
            // (1) Compute motor skip based on the ORIGINAL command.
            Func<FlowerUnit, bool>? shouldSkip = cmd.Id switch
            {
                "motor.open" => f => f.FlowerStatus == FlowerStatus.Open,
                "motor.close" => f => f.FlowerStatus == FlowerStatus.Closed,
                "motor.open.led.ramp" => f => f.FlowerStatus == FlowerStatus.Open,
                "motor.close.led.ramp" => f => f.FlowerStatus == FlowerStatus.Closed,
                _ => null
            };

            bool isNoOpNow = shouldSkip?.Invoke(flower) ?? false;

            // (2) If a combined motor+LED ramp is motor-no-op, rewrite to pure LED ramp.
            if (isNoOpNow && (cmd.Id == "motor.open.led.ramp" || cmd.Id == "motor.close.led.ramp"))
            {
                cmdId = "led.ramp";
                args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["endIntensity"] = args.TryGetValue("endIntensity", out var ei) ? ei : 0,
                    ["durationMs"] = args.TryGetValue("durationMs", out var dm) ? dm : 1500
                };
            }

            // Re-resolve command after any rewrite.
            if (cmdId != commandId)
                cmd = _commands.GetById(cmdId);

            // (3) IMPORTANT: Recompute shouldSkip *for the final command*.
            // LED-only commands must not inherit motor skip semantics.
            shouldSkip = cmd.Id switch
            {
                "motor.open" => f => f.FlowerStatus == FlowerStatus.Open,
                "motor.close" => f => f.FlowerStatus == FlowerStatus.Closed,
                _ => null
            };

            // Validate args for the final command.
            cmd.ValidateArgs(flower.Category, args);

            // Build frames + state transitions
            var frames = cmd.BuildPayload(flower.Id, flower.Category, args);

            // Base state mutations defined by the command
            var onAckBase = cmd.StateOnAck(flower.Category, args);

            // (4) Safety: LED commands must never flip Open/Close state.
            var onAck = cmd.Id.StartsWith("led.", StringComparison.OrdinalIgnoreCase)
                ? new Func<FlowerUnit, FlowerUnit>(f =>
                {
                    var before = f.FlowerStatus;
                    var updated = onAckBase(f);
                    updated.FlowerStatus = before; // preserve motor state
                    return updated;
                })
                : onAckBase;

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
