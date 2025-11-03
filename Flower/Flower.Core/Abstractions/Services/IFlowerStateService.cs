using Flower.Core.Enums;
using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Services
{
    public interface IFlowerStateService
    {
        Task ApplyAsync(int flowerId, Func<FlowerUnit, FlowerUnit> mutate);
        Task TouchConnectionAsync(int flowerId, ConnectionStatus status);
    }
}
