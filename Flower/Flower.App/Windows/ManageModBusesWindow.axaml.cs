using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Flower.App.ViewModels;

namespace Flower.App.Windows;

public partial class ManageModBusesWindow : Window
{
    public ManageModBusesWindow() => InitializeComponent();

    public ManageModBusesWindow(IManageModBusesViewModel vm) : this()
    {
        DataContext = vm;

        var closeBtn = this.FindControl<Button>("CloseButton");

        if (closeBtn is not null) closeBtn.Click += OnCloseClicked;
    }


    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is IManageBusesViewModel vm)
            vm.Close(); // keeps VM semantics

        Close(null);
    }
}