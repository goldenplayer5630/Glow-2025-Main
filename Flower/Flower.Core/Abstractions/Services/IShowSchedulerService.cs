using Flower.Core.Abstractions.Commands;
using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Services
{
    public interface IShowSchedulerService
    {

        Task PlayLoopResolvedAsync(IReadOnlyList<ShowEventResolved> events, int loopMs, CancellationToken ct);

        void Stop();
    }
}
