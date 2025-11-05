using Avalonia.Controls;
using Flower.App.ViewModels;
using Flower.App.Windows;
using Flower.Core.Abstractions.Services;
using Flower.Core.Models;
using System;
using System.Linq;
using System.Reactive;

namespace Flower.App.Windows
{
    public partial class MainWindow : Window
    {
        private readonly Func<ShowCreatorWindow> _showCreatorWindowFactory;
        private readonly Func<AddFlowerWindow> _addOrUpdateFlowerWindowFactory;
        private readonly Func<ManageBusesWindow> _manageBusesWindowFactory;
        private readonly IBusConfigService _busCfg;

        public MainWindow()
        {
            InitializeComponent();
            Width = MinWidth = MaxWidth = 1280;
            Height = MinHeight = MaxHeight = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        public MainWindow(
            IAppViewModel vm,
            Func<ShowCreatorWindow> showCreatorWindowFactory,
            Func<AddFlowerWindow> addFlowerWindowFactory,
            Func<ManageBusesWindow> manageBusesWindowFactory,
            IBusConfigService busCfg
        ) : this()
        {
            DataContext = vm;
            _showCreatorWindowFactory = showCreatorWindowFactory;
            _addOrUpdateFlowerWindowFactory = addFlowerWindowFactory;
            _manageBusesWindowFactory = manageBusesWindowFactory;
            _busCfg = busCfg;

            HookInteractions(vm);

            Width = MinWidth = MaxWidth = 1280;
            Height = MinHeight = MaxHeight = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void HookInteractions(IAppViewModel vmBase)
        {
            if (vmBase is not AppViewModel vm) return;

            vm.OpenShowCreatorInteraction.RegisterHandler(async ctx =>
            {
                var win = _showCreatorWindowFactory();
                await win.ShowDialog(this);
                ctx.SetOutput(Unit.Default);
            });

            // MainWindow.axaml.cs -> HookInteractions

            vm.AddFlowerInteraction.RegisterHandler(async ctx =>
            {
                var win = _addOrUpdateFlowerWindowFactory();

                if (win.DataContext is IAddOrUpdateFlowerViewModel addVm)
                    await addVm.InitAsync(null); // add mode

                if (win.DataContext is AddOrUpdateFlowerViewModel strongVm)
                {
                    void Handler(object? _, FlowerUnit? unit)
                    {
                        strongVm.CloseRequested -= Handler;
                        win.Close(unit);
                    }
                    strongVm.CloseRequested += Handler;
                }

                var result = await win.ShowDialog<FlowerUnit?>(this);
                ctx.SetOutput(result);
            });

            vm.UpdateFlowerInteraction.RegisterHandler(async ctx =>
            {
                var existing = ctx.Input;

                var win = _addOrUpdateFlowerWindowFactory(); // *** single window ***

                if (win.DataContext is IAddOrUpdateFlowerViewModel vm2)
                    await vm2.InitAsync(existing); // edit mode

                if (win.DataContext is AddOrUpdateFlowerViewModel strongVm)
                {
                    void Handler(object? _, FlowerUnit? unit)
                    {
                        strongVm.CloseRequested -= Handler;
                        win.Close(unit);
                    }
                    strongVm.CloseRequested += Handler;
                }

                var result = await win.ShowDialog<FlowerUnit?>(this);
                ctx.SetOutput(result);
            });

            vm.OpenManageBusesInteraction.RegisterHandler(async ctx =>
            {
                var win = _manageBusesWindowFactory(); // ← has DataContext from DI
                await win.ShowDialog(this);
                ctx.SetOutput(Unit.Default);
            });

            vm.AssignBusesInteraction.RegisterHandler(async ctx =>
            {
                var selectedFlowers = ctx.Input;
                var busIds = _busCfg.Buses.Select(b => b.BusId).ToList();

                if (busIds.Count == 0)
                {
                    // no buses configured: just return null
                    ctx.SetOutput(null);
                    return;
                }

                var win = new AssignBusWindow(busIds, selectedFlowers.Count);
                var result = await win.ShowDialog<string?>(this);
                ctx.SetOutput(result);
            });

        }
    }
}
