using Flower.Core.Abstractions.Stores;
using Flower.Core.Models;

namespace Flower.Infrastructure.Persistence
{
    public sealed class ShowProjectStore : JsonStoreBase<ShowProject>, IShowProjectStore
    {
        // Single source of truth for the folder path
        public static readonly string DefaultFolder =
            Path.Combine(AppContext.BaseDirectory, "json", "showprojects");

        // JsonStoreBase ctor will call Directory.CreateDirectory(Folder)
        protected override string Folder => DefaultFolder;

        // Up to you—this is only used when you call Save/Load with null
        protected override string DefaultFileName => "shows.json";
    }
}
