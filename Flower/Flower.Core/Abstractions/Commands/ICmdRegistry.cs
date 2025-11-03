using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Commands
{
    public interface ICommandRegistry
    {
        IEnumerable<IFlowerCommand> AllCommands { get; }
        IFlowerCommand GetById(string id);
        IEnumerable<IFlowerCommand> ForCategory(FlowerCategory category);
    }
}
