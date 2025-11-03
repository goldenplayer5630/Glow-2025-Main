using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Flower.Core.Abstractions.Services
{
    public interface IFlowerService
    {
        ReadOnlyObservableCollection<FlowerUnit> Flowers { get; }

        Task<IReadOnlyList<FlowerUnit>> GetAllAsync();
        Task LoadAsync(string? fileNameOrPath = "flowers.json");
        Task SaveAsync(string? fileNameOrPath = null);
        Task<FlowerUnit?> GetAsync(int id);
        Task AddAsync(FlowerUnit flower);
        Task<bool> UpdateAsync(FlowerUnit flower);
        Task<bool> DeleteAsync(int id);
    }
}
