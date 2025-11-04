using Flower.Core.Abstractions.Stores;
using Flower.Core.Models;
using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Persistence
{
    public sealed class BusConfigStore 
        : JsonStoreBase<List<BusConfig>>, IBusConfigStore
    {
        // <app>/json/busconfigs.json
        protected override string DefaultFileName => "busconfigs.json";
    }
}
