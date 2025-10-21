using Avalonia;
using Avalonia.ReactiveUI;
using Flower.App.ViewModels;
using Flower.App.Views;
using Flower.Core.Abstractions;
using Flower.Core.Cmds;
using Flower.Core.Cmds.BuiltIn;
using Flower.Core.Models.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;

namespace Flower.App.Desktop;

public class Program : Application
{
    [STAThread]
    public static int Main(string[] args)
    {
        // ----- RUNTIME path with DI -----
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

                // UI / VMs
                s.AddSingleton<IAppViewModel, AppViewModel>();
                s.AddSingleton<ShowCreatorViewModel>();


                // Views (runtime via DI; previewer uses parameterless ctors)
                s.AddSingleton<MainWindow>();
                s.AddTransient<ShowCreatorView>();
            })
            .Build();

        // ❗ Use the *renamed* runtime builder to avoid previewer confusion
        var builder = BuildAvaloniaAppWithHost(host);

        if (args.Contains("--drm"))
        {
            SilenceConsole();
            return builder.StartLinuxDrm(args, "/dev/dri/card1", 1D);
        }

        return builder.StartWithClassicDesktopLifetime(args);
    }

    // ✅ PREVIEWER expects exactly this method: public static AppBuilder BuildAvaloniaApp()
    // Keep ONLY this parameterless version with this exact name.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Flower.App.App>() // <-- your App class (namespace matters!)
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();

    // ✅ RUNTIME builder renamed so the previewer won't pick it up by name
    public static AppBuilder BuildAvaloniaAppWithHost(IHost host) =>
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
