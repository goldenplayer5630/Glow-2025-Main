using Avalonia;
using Avalonia.ReactiveUI;
using Bus.Core.Services;
using Flower.App.ViewModels;
using Flower.App.Views;
using Flower.App.Windows;
using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Abstractions.Stores;
using Flower.Core.Cmds;
using Flower.Core.Cmds.BuiltIn;
using Flower.Core.Services;
using Flower.Infrastructure.Io;
using Flower.Infrastructure.Persistence;
using Flower.Infrastructure.Protocol;
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
                // ============ Transport / Protocol ===========

                // Frame codec (use your real binary codec if you have one)
                s.AddSingleton<IFrameCodec, JsonLineCodec>();

                // Bus directory creates its own SerialPortTransport + ProtocolClient per bus
                s.AddSingleton<IBusDirectory, BusDirectory>();

                // ============ State & Domain ============
                // Flower store + service (authoritative collection)
                s.AddSingleton<IFlowerStore, FlowerStore>();
                s.AddSingleton<IFlowerService, FlowerService>();

                // Single-writer state mutator (used by dispatcher on ACK/timeout)
                s.AddSingleton<IFlowerStateService, FlowerStateService>();

                // Bus Service+ store
                s.AddSingleton<IBusConfigStore, BusConfigStore>();
                s.AddSingleton<IBusConfigService, BusConfigService>();

                // Commands (domain-level, human-readable Ids)
                s.AddSingleton<IFlowerCommand, LedSetCmd>();
                s.AddSingleton<IFlowerCommand, LedRampCmd>();
                s.AddSingleton<IFlowerCommand, MotorOpenCmd>();
                s.AddSingleton<IFlowerCommand, MotorCloseCmd>();
                s.AddSingleton<IFlowerCommand, MotorOpenLedRampCmd>();
                s.AddSingleton<IFlowerCommand, MotorCloseLedRamp>();
                s.AddSingleton<IFlowerCommand, PingCmd>();
                // (optional) s.AddSingleton<IFlowerCommand, InitCmd>();

                // Registry maps string ids <-> command instances (+ optional wire codes)
                s.AddSingleton<ICommandRegistry, CommandRegistry>();

                // ============ Show engine ============
                // Dispatcher (per-flower queues, send->wait ACK->update state)
                s.AddSingleton<ICmdDispatcher, CmdDispatcher>();

                // Scheduler drives timing; ShowPlayer builds dispatchable events
                s.AddSingleton<IShowSchedulerService, ShowSchedulerService>();
                s.AddSingleton<IShowPlayerService, ShowPlayerService>();
                s.AddSingleton<ICommandService, CommandService>();

                // Project store
                s.AddSingleton<IShowProjectStore, ShowProjectStore>();

                // ============ ViewModels ============
                s.AddSingleton<IAppViewModel, AppViewModel>();
                s.AddTransient<ISendCommandToflowerViewModel, SendCommandToFlowerViewModel>();
                s.AddTransient<IShowCreatorViewModel, ShowCreatorViewModel>();
                s.AddTransient<IAddOrUpdateFlowerViewModel, AddOrUpdateFlowerViewModel>();
                s.AddTransient<IManageBusesViewModel, ManageBusesViewModel>();
                s.AddTransient<IAssignBusViewModel, AssignBusViewModel>();

                // ============ Views / Windows ============
                s.AddTransient<ShowCreatorWindow>();
                s.AddTransient<AddFlowerWindow>();
                s.AddTransient<ManageBusesWindow>();
                s.AddTransient<AssignBusWindow>();
                s.AddTransient<SendCommandToFlowerWindow>();
                s.AddTransient<MainWindow>();

                // ============ Factories ============
                s.AddTransient<Func<ShowCreatorWindow>>(sp => () => sp.GetRequiredService<ShowCreatorWindow>());
                s.AddTransient<Func<AddFlowerWindow>>(sp => () => sp.GetRequiredService<AddFlowerWindow>());
                s.AddTransient<Func<ManageBusesWindow>>(sp => () => sp.GetRequiredService<ManageBusesWindow>());
                s.AddTransient<Func<AssignBusWindow>>(sp => () => sp.GetRequiredService<AssignBusWindow>());
                s.AddTransient<Func<SendCommandToFlowerWindow>>(sp => () => sp.GetRequiredService<SendCommandToFlowerWindow>());
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
