using Flower.Core.Abstractions.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Services
{
    public static class UiLogServiceExtensions
    {
        public static void Trace(this IUiLogService? log, string message)
        {
            if (log is null) return;
            log.Info("[TRACE] " + message);
        }

        public static void Warn(this IUiLogService? log, string message)
        {
            if (log is null) return;
            log.Info("[WARN] " + message);
        }

        public static void Error(this IUiLogService? log, string message, Exception? ex)
        {
            if (log is null) return;
            if (ex is null) log.Info("[ERROR] " + message);
            else log.Error(message, ex);
        }
    }
}
