using Flower.Core.Abstractions.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Persistence
{
    public abstract class JsonStoreBase<T> : IJsonStore<T> where T : new()
    {
        protected JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        // Default folder: <app>/json
        protected virtual string Folder => Path.Combine(AppContext.BaseDirectory, "json");

        // Each store sets its own default filename
        protected abstract string DefaultFileName { get; }

        protected JsonStoreBase()
        {
            Options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            Directory.CreateDirectory(Folder);
        }

        /// <summary>
        /// Load from default file, from a file name inside the json folder, or from an absolute/relative path.
        /// </summary>
        public async Task<T> LoadAsync(string? pathOrFileName = null)
        {
            var path = ResolvePath(pathOrFileName);
            if (!File.Exists(path))
                return new T();

            using var fs = File.OpenRead(path);
            return (await JsonSerializer.DeserializeAsync<T>(fs, Options)) ?? new T();
        }

        /// <summary>
        /// Save to default file, to a file name inside the json folder, or to a full path.
        /// </summary>
        public async Task SaveAsync(T value, string? pathOrFileName = null)
        {
            var path = ResolvePath(pathOrFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var fs = File.Create(path);
            await JsonSerializer.SerializeAsync(fs, value, Options);
        }

        protected string ResolvePath(string? pathOrFileName)
        {
            if (string.IsNullOrWhiteSpace(pathOrFileName))
                return Path.Combine(Folder, DefaultFileName);

            // If user provided a rooted path, use it as-is
            if (Path.IsPathRooted(pathOrFileName))
                return pathOrFileName;

            // If it contains any directory separators, treat as relative path
            if (pathOrFileName.Contains(Path.DirectorySeparatorChar) ||
                pathOrFileName.Contains(Path.AltDirectorySeparatorChar))
            {
                return Path.GetFullPath(pathOrFileName);
            }

            // Plain filename → put it in the json folder
            return Path.Combine(Folder, pathOrFileName);
        }
    }
}
