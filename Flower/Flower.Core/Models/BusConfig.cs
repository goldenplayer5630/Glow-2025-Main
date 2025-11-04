using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Records
{
    // Converted record to a regular model class with init-only properties and constructor.
    public sealed class BusConfig
    {
        public string BusId { get; init; }
        public string Port { get; init; }
        public int Baud { get; init; }

        public BusConfig(string busId, string port, int baud)
        {
            BusId = busId ?? throw new ArgumentNullException(nameof(busId));
            Port = port ?? throw new ArgumentNullException(nameof(port));
            Baud = baud;
        }
    }
}
