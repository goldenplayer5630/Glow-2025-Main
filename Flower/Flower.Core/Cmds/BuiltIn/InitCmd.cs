using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Cmds.BuiltIn
{
    internal class InitCmd : IFlowerCommand
    {
        public string Id => throw new NotImplementedException();

        public string DisplayName => throw new NotImplementedException();

        public IReadOnlyCollection<FlowerCategory> SupportedCategories => throw new NotImplementedException();

        public IReadOnlyList<byte[]> BuildFrames(int flowerId, FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            throw new NotImplementedException();
        }

        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            throw new NotImplementedException();
        }
    }
}
