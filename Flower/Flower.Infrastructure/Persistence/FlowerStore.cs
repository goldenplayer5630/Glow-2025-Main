using Flower.Core.Abstractions.Stores;
using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Persistence
{
    public sealed class FlowerStore
        : JsonStoreBase<List<FlowerUnit>>, IFlowerStore
    {
        // <app>/json/flowers.json
        protected override string DefaultFileName => "flowers.json";
    }
}
