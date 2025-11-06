using Flower.Core.Enums;
using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Commands
{
    public interface ICommandService
    {
        Task<CommandOutcome> SendCommandAsync(
            string commandId,
            FlowerUnit flowerUnit,
            IReadOnlyDictionary<string, object> args,
            CancellationToken ct = default);
    }
}
