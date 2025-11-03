using Avalonia.Controls;
using System;
using System.Reactive;
using Flower.App.ViewModels;
using Flower.App.Windows;
using Flower.Core.Models;

namespace Flower.App.Windows
{
    public partial class MainWindow : Window
    {
        private readonly Func<ShowCreatorWindow> _showCreatorWindowFactory;
        private readonly Func<AddFlowerWindow> _addOrUpdateFlowerWindowFactory;

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
            Func<AddFlowerWindow> addFlowerWindowFactory
        ) : this()
        {
            DataContext = vm;
            _showCreatorWindowFactory = showCreatorWindowFactory;
            _addOrUpdateFlowerWindowFactory = addFlowerWindowFactory;

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


        }
    }
}
