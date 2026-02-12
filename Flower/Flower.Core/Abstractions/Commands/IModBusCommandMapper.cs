using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Commands
{
    public interface IModBusCommandMapper
    {
        ModbusCommandMap Map(CommandRequest req);
    }
}
