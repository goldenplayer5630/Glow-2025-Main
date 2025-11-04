using Flower.Core.Models;
using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Commands
{
    public interface IBusDirectory : IAsyncDisposable
    {
        Task OpenAsync(IEnumerable<BusConfig> configs);
        IProtocolClient GetProtocol(string busId);
        Task ConnectAsync(BusConfig config);
        Task DisconnectAsync(string busId);
        bool IsOpen(string busId);
    }
}
