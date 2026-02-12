using Flower.Core.Models;

namespace Flower.Core.Abstractions.Services
{
    public interface IModBusConnectionTester
    {
        Task<(bool ok, string message)> TestAsync(BusConfig bus, CancellationToken ct = default);
    }
}
