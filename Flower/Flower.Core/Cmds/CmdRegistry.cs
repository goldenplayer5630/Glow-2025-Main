using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Cmds
{
    public sealed class CommandRegistry : ICommandRegistry
    {
        private readonly Dictionary<string, IFlowerCommand> _byId;

        public CommandRegistry(IEnumerable<IFlowerCommand> commands)
        {
            _byId = commands.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<IFlowerCommand> AllCommands => _byId.Values;

        public IFlowerCommand GetById(string id)
            => _byId.TryGetValue(id, out var cmd)
                ? cmd
                : throw new KeyNotFoundException($"Unknown command '{id}'.");

        public IEnumerable<IFlowerCommand> ForCategory(FlowerCategory category)
            => _byId.Values.Where(c => c.SupportedCategories.Contains(category));
    }
}
