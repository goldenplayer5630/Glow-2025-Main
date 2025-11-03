using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Enums
{
    public enum ProtocolMessageType : byte { Command = 0, Ack = 1, Nack = 2, Event = 3, Heartbeat = 4 }
}
