using Flower.Core.Abstractions.Stores;
using Flower.Infrastructure.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Xml;

namespace Flower.Infrastructure.Persistence
{
    public abstract class JsonStoreBase<T> : IJsonStore<T> where T : new()
    {
        // Newtonsoft settings (camelCase + enums as strings, indented output)
        protected JsonSerializerSettings Settings { get; } = new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        // Default folder: <app>/json
        protected virtual string Folder => Path.Combine(AppContext.BaseDirectory, "json");

        // Each store sets its own default filename
        protected abstract string DefaultFileName { get; }

        protected JsonStoreBase()
        {
            // Prefer string enums (camelCase)
            Settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy(), allowIntegerValues: true));

            Settings.Converters.Insert(0, new PreferInt32ObjectConverter());

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

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json, Settings) ?? new T();
        }

        /// <summary>
        /// Save to default file, to a file name inside the json folder, or to a full path.
        /// </summary>
        public async Task SaveAsync(T value, string? pathOrFileName = null)
        {
            var path = ResolvePath(pathOrFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var json = JsonConvert.SerializeObject(value, Settings);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }

        protected string ResolvePath(string? pathOrFileName)
        {
            if (string.IsNullOrWhiteSpace(pathOrFileName))
                return Path.Combine(Folder, DefaultFileName);

            if (Path.IsPathRooted(pathOrFileName))
                return pathOrFileName;

            if (pathOrFileName.Contains(Path.DirectorySeparatorChar) ||
                pathOrFileName.Contains(Path.AltDirectorySeparatorChar))
            {
                return Path.GetFullPath(pathOrFileName);
            }

            return Path.Combine(Folder, pathOrFileName);
        }
    }
}
