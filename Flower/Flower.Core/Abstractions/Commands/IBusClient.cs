using Flower.Core.Enums;
using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Commands
{
    public interface IBusClient : IAsyncDisposable
    {
        bool IsOpen { get; }

        /// <summary>
        /// Send a command request over this bus (serial or modbus).
        /// Must return a CommandOutcome consistent with the dispatcher.
        /// </summary>
        Task<CommandOutcome> SendAsync(CommandRequest req, CancellationToken ct = default);
    }
}
