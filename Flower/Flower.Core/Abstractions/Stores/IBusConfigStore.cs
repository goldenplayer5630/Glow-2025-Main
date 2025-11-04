using Flower.Core.Models;
using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Stores
{
    public interface IBusConfigStore : IJsonStore<List<BusConfig>>
    {
    }
}
