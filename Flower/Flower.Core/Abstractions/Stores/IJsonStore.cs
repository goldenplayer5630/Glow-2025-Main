using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Stores
{
    // Generic JSON store contract
    public interface IJsonStore<T>
    {
        Task<T> LoadAsync(string? pathOrFileName = null);
        Task SaveAsync(T value, string? pathOrFileName = null);
    }
}
