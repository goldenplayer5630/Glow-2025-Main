using Serilog;
using System.IO;

namespace Flower.Infrastructure.Logging;

public static class SerilogConfig
{
    //public static ILogger Create(string logDir)
    //{
    //    Directory.CreateDirectory(logDir);

    //    return new LoggerConfiguration()
    //        .MinimumLevel.Information()
    //        .WriteTo.File(
    //            Path.Combine(logDir, "app-.log"),
    //            rollingInterval: RollingInterval.Day)   // <- enum comes from Serilog
    //        .CreateLogger();
    //}
}
