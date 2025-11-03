using Flower.Core.Abstractions.Stores;
using Flower.Core.Models;

namespace Flower.Infrastructure.Persistence
{
    public sealed class ShowProjectStore : JsonStoreBase<ShowProject>, IShowProjectStore
    {
        protected override string DefaultFileName => "show.json";
    }
}
