using Flower.Core.Enums;
using Newtonsoft.Json;

namespace Flower.Core.Models
{
    [JsonObject(MemberSerialization.OptOut)]
    public sealed class BusConfig
    {
        public string BusId { get; set; }
        public string Port { get; set; }
        public int Baud { get; set; }
        [JsonIgnore]
        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Disconnected;

        public BusConfig(string busId, string port, int baud, ConnectionStatus connectionStatus)
        {
            BusId = busId ?? throw new ArgumentNullException(nameof(busId));
            Port = port ?? throw new ArgumentNullException(nameof(port));
            Baud = baud;
            ConnectionStatus = ConnectionStatus.Disconnected;
        }
    }
}
