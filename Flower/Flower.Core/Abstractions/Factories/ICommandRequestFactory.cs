using Flower.Core.Models;
using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Factories
{
    public interface ICommandRequestFactory
    {
        CommandRequest BuildFor(
            FlowerUnit flower,
            string commandId,
            IReadOnlyDictionary<string, object?>? rawArgs,
            TimeSpan? ackTimeout = null);
    }
}
