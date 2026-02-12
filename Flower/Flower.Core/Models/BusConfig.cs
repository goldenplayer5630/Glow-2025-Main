using Flower.Core.Enums;
using Newtonsoft.Json;
using System.Runtime.InteropServices.JavaScript;

namespace Flower.Core.Models
{
    [JsonObject(MemberSerialization.OptOut)]
    public sealed class BusConfig
    {
        public string BusId { get; set; }
        public BusType BusType { get; set; } = BusType.SerialBus;
        
        // -------- Serial settings (existing) --------
        public string? Port { get; set; }
        public int Baud { get; set; }

        // -------- Modbus TCP settings (new) --------
        public string? ModbusHost { get; set; }
        public int ModbusPort { get; set; } = 502;
        public byte ModbusUnitId { get; set; } = 1;
        public int ModbusConnectTimeoutMs { get; set; } = 2000;

        [JsonIgnore]
        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Disconnected;

        public BusConfig(string busId)
        {
            BusId = busId ?? throw new ArgumentNullException(nameof(busId));
        }

        // Optional convenience factories
        public void CreateSerial(string port, int baud)
        {
            BusType = BusType.SerialBus;
            Port = port;
            Baud = baud;
        }

        public void CreateModbusTcp(string host, int port = 502, byte unitId = 1)
        {
            BusType = BusType.ModbusTcp;
            ModbusHost = host;
            ModbusPort = port;
            ModbusUnitId = unitId;
        }
    }
}
