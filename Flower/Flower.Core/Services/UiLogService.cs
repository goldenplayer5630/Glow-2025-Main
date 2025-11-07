using Flower.Core.Abstractions.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Services
{
    public sealed class UiLogService : IUiLogService
    {
        // AppViewModel can subscribe to these to render in the textbox
        public event Action<string>? InfoPublished;
        public event Action<string, Exception>? ErrorPublished;

        public void Info(string message) => InfoPublished?.Invoke(message);
        public void Error(string message, Exception ex) => ErrorPublished?.Invoke(message, ex);
    }
}
