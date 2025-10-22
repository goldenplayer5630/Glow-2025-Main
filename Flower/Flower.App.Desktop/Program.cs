using Avalonia;
using Avalonia.ReactiveUI;
using Flower.App.ViewModels;
using Flower.App.Views;
using Flower.App.Windows;
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

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(s =>
            {
                // Engine / Core
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

                // VMs
                s.AddSingleton<IAppViewModel, AppViewModel>();
                s.AddTransient<IShowCreatorViewModel, ShowCreatorViewModel>();

                // Funcs
                s.AddTransient<Func<ShowCreatorWindow>>(sp => () => sp.GetRequiredService<ShowCreatorWindow>());

                // Views / Windows
                s.AddTransient<ShowCreatorWindow>();
                s.AddTransient<MainWindow>();

            })
            .Build();

        var builder = BuildAvaloniaAppWithHost(host);

        if (args.Contains("--drm"))
        {
            SilenceConsole();
            return builder.StartLinuxDrm(args, "/dev/dri/card1", 1D);
        }

        return builder.StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Flower.App.App>() // <-- your App class (namespace matters!)
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();

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
