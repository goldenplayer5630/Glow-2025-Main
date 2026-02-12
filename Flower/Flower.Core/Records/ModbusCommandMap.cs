namespace Flower.Core.Records
{
    public enum ModbusWriteKind
    {
        WriteSingleCoil,
        WriteMultipleCoils,
        WriteSingleRegister,
        WriteMultipleRegisters
    }

    public sealed record ModbusCommandMap(
        ModbusWriteKind Kind,
        int Address,
        bool[]? Coils = null,
        ushort[]? Registers = null
    );
}
