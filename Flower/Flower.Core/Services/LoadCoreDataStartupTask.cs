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
        private readonly IShowProjectService _showProjectService;

        public LoadCoreDataStartupTask(
            IFlowerService flowerService,
            IBusConfigService busConfigService,
            IShowProjectService showProjectService)
        {
            _flowerService = flowerService;
            _busConfigService = busConfigService;
            _showProjectService = showProjectService;
        }

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            // These are idempotent: calling again is safe
            await _busConfigService.LoadAsync();
            await _flowerService.LoadAsync();
            await _showProjectService.LoadAsync();
        }
    }

}
