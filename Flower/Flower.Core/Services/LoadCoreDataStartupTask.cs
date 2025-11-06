using Flower.Core.Abstractions.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Services
{
    public sealed class LoadCoreDataStartupTask : IStartupTask
    {
        private readonly IFlowerService _flowerService;
        private readonly IBusConfigService _busConfigService;
        private readonly IShowPlayerService _showPlayerService;

        public LoadCoreDataStartupTask(
            IFlowerService flowerService,
            IBusConfigService busConfigService,
            IShowPlayerService showPlayerService)
        {
            _flowerService = flowerService;
            _busConfigService = busConfigService;
            _showPlayerService = showPlayerService;
        }

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            // These are idempotent: calling again is safe
            await _busConfigService.LoadAsync();
            await _flowerService.LoadAsync();
            // await _showPlayerService.LoadAsync().ConfigureAwait(false); not implemented yet
        }
    }

}
