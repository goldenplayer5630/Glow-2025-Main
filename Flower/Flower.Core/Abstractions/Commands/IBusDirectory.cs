using Flower.Core.Abstractions.Commands;
using Flower.Core.Models;

namespace Flower.Core.Abstractions.Commands
{
    public interface IBusDirectory : IAsyncDisposable
    {
        Task OpenAsync(IEnumerable<BusConfig> configs);
        Task ConnectAsync(BusConfig cfg);
        Task DisconnectAsync(string busId);

        bool IsOpen(string busId);

        IBusClient GetClient(string busId);
    }
}
