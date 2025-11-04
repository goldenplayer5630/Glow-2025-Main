using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Models
{
    // Converted record to a regular model class with init-only properties and constructor.
    public sealed class BusConfig
    {
        public string BusId { get; set; }
        public string Port { get; set; }
        public int Baud { get; set; }
        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Disconnected;

        public BusConfig(string busId, string port, int baud)
        {
            BusId = busId ?? throw new ArgumentNullException(nameof(busId));
            Port = port ?? throw new ArgumentNullException(nameof(port));
            Baud = baud;
        }
    }
}
