using Flower.Core.Abstractions.Stores;
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
    public interface IShowProjectService : IDisposable
    {
        ReadOnlyObservableCollection<ShowProject> Projects { get; }

        Task<IReadOnlyList<ShowProject>> GetAllAsync();

        Task LoadAsync(string? fileNameOrPath = "shows.json");

        Task SaveAsync(string? fileNameOrPath = null);

        Task<ShowProject?> GetAsync(string id);

        Task AddAsync(ShowProject project);

        Task<bool> UpdateAsync(ShowProject project);

        Task<bool> DeleteAsync(string id);
    }
}
