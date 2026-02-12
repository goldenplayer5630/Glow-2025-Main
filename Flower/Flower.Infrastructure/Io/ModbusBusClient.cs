using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using Flower.Core.Records;

namespace Flower.Infrastructure.Io
{
    public sealed class ModbusBusClient : IBusClient
    {
        private readonly ModbusTcpClientTransport _transport;
        private readonly IModBusCommandMapper _mapper;

        public bool IsOpen => _transport.IsConnected;

        public ModbusBusClient(ModbusTcpClientTransport transport, IModBusCommandMapper mapper)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<CommandOutcome> SendAsync(CommandRequest req, CancellationToken ct = default)
        {
            try
            {
                var plan = _mapper.Map(req);
                ct.ThrowIfCancellationRequested();

                switch (plan.Kind)
                {
                    case ModbusWriteKind.WriteSingleCoil:
                        {
                            if (plan.Coils is null || plan.Coils.Length != 1)
                                throw new InvalidOperationException("WriteSingleCoil requires Coils length == 1.");

                            await _transport.WriteCoilAsync(plan.Address, plan.Coils[0], ct).ConfigureAwait(false);
                            break;
                        }

                    case ModbusWriteKind.WriteMultipleCoils:
                        {
                            if (plan.Coils is null || plan.Coils.Length == 0)
                                throw new InvalidOperationException("WriteMultipleCoils requires Coils length > 0.");

                            // No bulk API in your transport? Write sequentially.
                            for (int i = 0; i < plan.Coils.Length; i++)
                            {
                                ct.ThrowIfCancellationRequested();
                                await _transport.WriteCoilAsync(plan.Address + i, plan.Coils[i], ct).ConfigureAwait(false);
                            }
                            break;
                        }

                    case ModbusWriteKind.WriteSingleRegister:
                        {
                            if (plan.Registers is null || plan.Registers.Length != 1)
                                throw new InvalidOperationException("WriteSingleRegister requires Registers length == 1.");

                            await _transport.WriteRegisterAsync(plan.Address, plan.Registers[0], ct).ConfigureAwait(false);
                            break;
                        }

                    case ModbusWriteKind.WriteMultipleRegisters:
                        {
                            if (plan.Registers is null || plan.Registers.Length == 0)
                                throw new InvalidOperationException("WriteMultipleRegisters requires Registers length > 0.");

                            // No bulk API in your transport? Write sequentially.
                            for (int i = 0; i < plan.Registers.Length; i++)
                            {
                                ct.ThrowIfCancellationRequested();
                                await _transport.WriteRegisterAsync(plan.Address + i, plan.Registers[i], ct).ConfigureAwait(false);
                            }
                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException(nameof(plan.Kind), plan.Kind, "Unknown ModbusWriteKind");
                }

                return CommandOutcome.Acked;
            }
            catch (OperationCanceledException)
            {
                return CommandOutcome.Acked; // keep your existing semantics
            }
            catch
            {
                return CommandOutcome.Timeout;
            }
        }

        public ValueTask DisposeAsync()
            => _transport.DisposeAsync();
    }
}
