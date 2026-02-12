using Avalonia;
using Bus.Core.Services;
using EasyModbus;
using Flower.App.ViewModels;
using Flower.App.Windows;
using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Factories;
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
using ReactiveUI.Avalonia;
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
                s.AddSingleton<IFrameCodec, JsonLineCodec>();

                // Bus directory: make sure this version supports BOTH Serial + Modbus
                s.AddSingleton<IBusDirectory, BusDirectory>();
                s.AddSingleton<IModBusCommandMapper, ModbusCommandMapper>();

                // Optional: UI "Test connection" button support
                s.AddSingleton<IModBusConnectionTester, ModBusConnectionTester>();

                // If your BusDirectory needs to construct transports via DI instead of new()
                // register transport(s) too:
                s.AddTransient<SerialPortTransport>();
                s.AddTransient<ModbusTcpClientTransport>();

                // ============ State & Domain ============
                s.AddSingleton<IFlowerStore, FlowerStore>();
                s.AddSingleton<IFlowerService, FlowerService>();
                s.AddSingleton<IFlowerStateService, FlowerStateService>();

                // Bus Service + store
                s.AddSingleton<IBusConfigStore, BusConfigStore>();
                s.AddSingleton<IBusConfigService, BusConfigService>();

                // ============ Commands ============
                s.AddSingleton<IFlowerCommand, LedSetCmd>();
                s.AddSingleton<IFlowerCommand, LedRampCmd>();
                s.AddSingleton<IFlowerCommand, LedRampInCmd>();
                s.AddSingleton<IFlowerCommand, LedRampOutCmd>();
                s.AddSingleton<IFlowerCommand, RgbCmd>();
                s.AddSingleton<IFlowerCommand, RgbInnerCmd>();
                s.AddSingleton<IFlowerCommand, RgbOuterCmd>();
                s.AddSingleton<IFlowerCommand, MotorOpenCmd>();
                s.AddSingleton<IFlowerCommand, MotorCloseCmd>();
                s.AddSingleton<IFlowerCommand, MotorOpenLedRampCmd>();
                s.AddSingleton<IFlowerCommand, MotorCloseLedRamp>();
                s.AddSingleton<IFlowerCommand, MotorStopCmd>();
                s.AddSingleton<IFlowerCommand, PingCmd>();

                s.AddSingleton<ICommandRegistry, CommandRegistry>();
                s.AddSingleton<ICommandRequestFactory, CommandRequestFactory>();

                // ============ Show engine ============
                s.AddSingleton<ICmdDispatcher, CmdDispatcher>();
                s.AddSingleton<IShowSchedulerService, ShowSchedulerService>();
                s.AddSingleton<IShowPlayerService, ShowPlayerService>();
                s.AddSingleton<ICommandService, CommandService>();

                // Project store
                s.AddSingleton<IShowProjectStore, ShowProjectStore>();
                s.AddSingleton<IShowProjectService, ShowProjectService>();

                // ============ Startup Tasks ============
                s.AddSingleton<IStartupTask, LoadCoreDataStartupTask>();

                // ============ ViewModels ============
                s.AddSingleton<IAppViewModel, AppViewModel>();
                s.AddTransient<ISendCommandToflowerViewModel, SendCommandToFlowerViewModel>();
                s.AddTransient<IShowCreatorViewModel, ShowCreatorViewModel>();
                s.AddTransient<IAddOrUpdateFlowerViewModel, AddOrUpdateFlowerViewModel>();
                s.AddTransient<IManageBusesViewModel, ManageSerialBusesViewModel>();
                s.AddTransient<IManageModBusesViewModel, ManageModBusesViewModel>();
                s.AddTransient<IAssignBusViewModel, AssignBusViewModel>();
                s.AddTransient<ILoadShowViewModel, LoadShowViewModel>();

                // ============ Views / Windows ============
                s.AddTransient<ShowCreatorWindow>();
                s.AddTransient<AddFlowerWindow>();
                s.AddTransient<ManageSerialBusesWindow>();
                s.AddTransient<ManageModBusesWindow>();
                s.AddTransient<AssignBusWindow>();
                s.AddTransient<SendCommandToFlowerWindow>();
                s.AddTransient<LoadShowWindow>();
                s.AddTransient<MainWindow>();

                // ============ Factories ============
                s.AddTransient<Func<ShowCreatorWindow>>(sp => () => sp.GetRequiredService<ShowCreatorWindow>());
                s.AddTransient<Func<AddFlowerWindow>>(sp => () => sp.GetRequiredService<AddFlowerWindow>());
                s.AddTransient<Func<ManageSerialBusesWindow>>(sp => () => sp.GetRequiredService<ManageSerialBusesWindow>());
                s.AddTransient<Func<ManageModBusesWindow>>(sp => () => sp.GetRequiredService<ManageModBusesWindow>());
                s.AddTransient<Func<AssignBusWindow>>(sp => () => sp.GetRequiredService<AssignBusWindow>());
                s.AddTransient<Func<SendCommandToFlowerWindow>>(sp => () => sp.GetRequiredService<SendCommandToFlowerWindow>());

                // ============ Cross-cutting ============
                s.AddSingleton<IUiLogService, UiLogService>();
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
