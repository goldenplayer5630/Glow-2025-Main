using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Services
{
    public interface IStartupTask
    {
        Task ExecuteAsync(CancellationToken ct = default);
    }
}
