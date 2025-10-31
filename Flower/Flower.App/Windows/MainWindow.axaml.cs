// Flower.App/Views/MainWindow.axaml.cs
using Avalonia.Controls;
using System.Threading.Tasks;
using Flower.App.ViewModels;
using System.Reactive;
using System;

namespace Flower.App.Windows;

public partial class MainWindow : Window
{
    private readonly System.Func<ShowCreatorWindow> _showCreatorWindowFactory;

    private MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(IAppViewModel vm, Func<ShowCreatorWindow> factory) : this()
    {
        DataContext = vm;
        _showCreatorWindowFactory = factory;
        HookInteractions(vm);
    }

    private void HookInteractions(IAppViewModel vmBase)
    {
        if (vmBase is AppViewModel vm)
        {
            vm.OpenShowCreatorInteraction.RegisterHandler(async ctx =>
            {
                var win = _showCreatorWindowFactory();      // DI creates the window
                // non-modal:
                win.Show(this);
                // await win.ShowDialog(this);
                ctx.SetOutput(Unit.Default);
            });
        }
    }
}
