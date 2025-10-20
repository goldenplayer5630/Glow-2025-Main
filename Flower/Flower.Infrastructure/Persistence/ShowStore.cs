using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Persistence
{
    public class ShowStore
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public static async Task<ShowProject> LoadAsync(string path)
        {
            using var fs = File.OpenRead(path);
            return (await JsonSerializer.DeserializeAsync<ShowProject>(fs, Options)) ?? new ShowProject();
        }

        public static async Task SaveAsync(string path, ShowProject project)
        {
            using var fs = File.Create(path);
            await JsonSerializer.SerializeAsync(fs, project, Options);
        }
    }
}
