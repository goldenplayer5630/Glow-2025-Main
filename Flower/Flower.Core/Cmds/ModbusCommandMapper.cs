using Flower.Core.Abstractions.Commands;
using Flower.Core.Records;

namespace Flower.Core.Cmds
{
    /// <summary>
    /// Default mapping from your command ids to Modbus coil/register writes.
    /// Adjust addresses to your relay/PLC mapping.
    /// </summary>
    public sealed class ModbusCommandMapper : IModBusCommandMapper
    {
        public ModbusCommandMap Map(CommandRequest req)
        {
            // Example strategy:
            // - Use CommandId to select coil/register
            // - Optionally use req.FlowerId as offset (e.g., per-channel relay)
            //
            // IMPORTANT: replace mapping with your real address layout.

            var channel = req.FlowerId <= 0 ? 0 : (req.FlowerId - 1); // flowerId 1 => channel 0

            return req.CommandId switch
            {
                // Motor relay example: open/close on coils
                "motor.open" => new ModbusCommandMap(ModbusWriteKind.WriteSingleCoil, Address: 0 + channel, Coils: new[] { true }),
                "motor.close" => new ModbusCommandMap(ModbusWriteKind.WriteSingleCoil, Address: 0 + channel, Coils: new[] { false }),

                // Ping doesn't “do” anything on a relay; you might treat it as no-op
                "ping" => new ModbusCommandMap(ModbusWriteKind.WriteSingleCoil, Address: 0 + channel, Coils: new[] { false }),

                // Default: throw so you see missing mappings fast
                _ => throw new KeyNotFoundException($"No Modbus mapping for command '{req.CommandId}'.")
            };
        }
    }
}
