using Flower.Core.Models;
using System.Collections.ObjectModel;
namespace Flower.Core.Abstractions.Services
{
    public interface IBusConfigService
    {
        ReadOnlyObservableCollection<BusConfig> Buses { get; }
        Task<IReadOnlyList<BusConfig>> GetAllAsync();
        Task LoadAsync(string? fileNameOrPath = "buses.json");
        Task SaveAsync(string? fileNameOrPath = "buses.json");
        Task<BusConfig?> GetAsync(string id);
        Task AddAsync(BusConfig Bus);
        Task<bool> UpdateAsync(BusConfig Bus);
        Task<bool> DeleteAsync(string id);
    }
}