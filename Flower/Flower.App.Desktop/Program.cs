using Avalonia;
using Avalonia.ReactiveUI;
using Flower.App.ViewModels;
using Flower.App.Views;
using Flower.Core.Abstractions;
using Flower.Core.Cmds;
using Flower.Core.Cmds.BuiltIn;
using Flower.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;

namespace Flower.App.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(s =>
            {
                // engine
                s.AddSingleton<ISerialPort, SerialPortService>();
                s.AddSingleton<ShowScheduler>();
                s.AddSingleton<ShowPlayer>();
                s.AddSingleton<IFlowerCommand, LedSetCmd>();
                s.AddSingleton<IFlowerCommand, LedRampCmd>();
                s.AddSingleton<IFlowerCommand, MotorOpenCmd>();
                s.AddSingleton<IFlowerCommand, MotorCloseCmd>();
                s.AddSingleton<IFlowerCommand, MotorOpenLedRampCmd>();
                s.AddSingleton<IFlowerCommand, MotorCloseLedRamp>();
                s.AddSingleton<ICommandRegistry, CommandRegistry>();
                // UI
                s.AddSingleton<MainViewModel>();
                s.AddSingleton<MainWindow>();
                s.AddSingleton<MainView>();
                s.AddSingleton<IAppViewModel, AppViewModel>();

            })
            .Build();

        var builder = BuildAvaloniaApp(host);

        if (args.Contains("--drm"))
        {
            SilenceConsole();
            return builder.StartLinuxDrm(args, "/dev/dri/card1", 1D);
        }

        return builder.StartWithClassicDesktopLifetime(args);
    }

    // ✅ use the factory overload to construct App with your host
    public static AppBuilder BuildAvaloniaApp(IHost host) =>
        AppBuilder.Configure(() => new Flower.App.App(host))
                  .UsePlatformDetect()
                  .WithInterFont()
                  .LogToTrace()
                  .UseReactiveUI();

    private static void SilenceConsole()
    {
        new Thread(() =>
        {
            Console.CursorVisible = false;
            while (true) Console.ReadKey(true);
        })
        { IsBackground = true }.Start();
    }
}
