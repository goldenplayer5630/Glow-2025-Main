using Avalonia.Controls;
using Flower.App.ViewModels;
using Flower.App.Windows;
using Flower.Core.Abstractions.Services;
using Flower.Core.Abstractions.Stores;
using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace Flower.App.Windows
{
    public partial class MainWindow : Window
    {
        private readonly Func<ShowCreatorWindow> _showCreatorWindowFactory;
        private readonly Func<AddFlowerWindow> _addOrUpdateFlowerWindowFactory;
        private readonly Func<ManageSerialBusesWindow> _manageBusesWindowFactory;
        private readonly Func<ManageModBusesWindow> _manageModBusesWindowFactory;
        private readonly Func<SendCommandToFlowerWindow> _sendCommandToFlowerWindowFactory;
        private readonly IBusConfigService _busCfg;
        private readonly IShowProjectStore _showStore;

        public MainWindow()
        {
            InitializeComponent();
            Width = MinWidth = MaxWidth = 800;
            Height = MinHeight = MaxHeight = 480;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        public MainWindow(
            IAppViewModel vm,
            Func<ShowCreatorWindow> showCreatorWindowFactory,
            Func<AddFlowerWindow> addFlowerWindowFactory,
            Func<ManageSerialBusesWindow> manageBusesWindowFactory,
            Func<ManageModBusesWindow> manageModBusesWindowFactory,
            Func<SendCommandToFlowerWindow> sendCommandToFlowerWindowFactory,
            IBusConfigService busCfg,
            IShowProjectStore showStore
        ) : this()
        {
            DataContext = vm;
            _showCreatorWindowFactory = showCreatorWindowFactory;
            _addOrUpdateFlowerWindowFactory = addFlowerWindowFactory;
            _manageBusesWindowFactory = manageBusesWindowFactory;
            _manageModBusesWindowFactory = manageModBusesWindowFactory;
            _sendCommandToFlowerWindowFactory = sendCommandToFlowerWindowFactory;
            _busCfg = busCfg;
            _showStore = showStore ?? throw new ArgumentNullException(nameof(showStore));

            HookInteractions(vm);

            Width = MinWidth = MaxWidth = 800;
            Height = MinHeight = MaxHeight = 480;
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

            vm.OpenManageSerialBusInteraction.RegisterHandler(async ctx =>
            {
                var win = _manageBusesWindowFactory(); // ← has DataContext from DI
                await win.ShowDialog(this);
                ctx.SetOutput(Unit.Default);
            });

            vm.OpenManageModBusInteraction.RegisterHandler(async ctx =>
            {
                var win = _manageModBusesWindowFactory(); // ← has DataContext from DI
                await win.ShowDialog(this);
                ctx.SetOutput(Unit.Default);
            });

            vm.AssignBusesInteraction.RegisterHandler(async ctx =>
            {
                var selectedFlowers = ctx.Input; // IReadOnlyList<FlowerUnit>
                var busIds = _busCfg.Buses.Select(b => b.BusId).ToList();

                var assignVm = new AssignBusViewModel(busIds, selectedFlowers.Count);

                var win = new AssignBusWindow(busIds, selectedFlowers.Count);

                // Show the modal dialog and return the selected bus id (or null on cancel)
                var result = await win.ShowDialog<string?>(this);
                ctx.SetOutput(result);
            });

            vm.SendCommandToFlowerInteraction.RegisterHandler(async ctx =>
            {
                var flower = ctx.Input; // FlowerUnit
                if (flower is null) { ctx.SetOutput(null); return; }

                var win = _sendCommandToFlowerWindowFactory();

                if (win.DataContext is ISendCommandToflowerViewModel vm2)
                    await vm2.InitAsync(flower);

                var result = await win.ShowDialog<string?>(this);
                ctx.SetOutput(result);
            });

            vm.LoadShowInteraction.RegisterHandler(async ctx =>
            {
                if (_showStore is null)
                    throw new InvalidOperationException("MainWindow has no IShowProjectStore.");

                var vmLoad = new LoadShowViewModel(_showStore, this);   // <-- non-null
                var win = new LoadShowWindow { DataContext = vmLoad };

                await vmLoad.InitAsync(); // optional: pass a known folder

                var result = await win.ShowDialog<ShowProject?>(this);
                ctx.SetOutput(result);
            });


        }
    }
}
