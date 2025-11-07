// Flower.App.Windows/SendCommandToFlowerWindow.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Flower.App.ViewModels;
using System;

namespace Flower.App.Windows;

public partial class SendCommandToFlowerWindow : Window
{
    private readonly ISendCommandToflowerViewModel _vm;

    public SendCommandToFlowerWindow(ISendCommandToflowerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;

        // Wire buttons
        this.FindControl<Button>("ConfirmButton")!.Click += OnConfirmClicked;
        this.FindControl<Button>("CloseButton")!.Click += OnCloseClicked;

        // Listen for VM close requests
        _vm.CloseRequested += OnVmCloseRequested;

        // Avoid leaks
        this.Closed += (_, __) => _vm.CloseRequested -= OnVmCloseRequested;
    }

    private async void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ISendCommandToflowerViewModel vm)
            await vm.ConfirmAsync(); // VM will raise CloseRequested based on CloseAfterSend
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ISendCommandToflowerViewModel vm)
            vm.Cancel(); // triggers CloseRequested(null)
        else
            Close(null);
    }

    private void OnVmCloseRequested(object? sender, string? commandId)
    {
        // Always marshal to UI thread to close safely
        Dispatcher.UIThread.Post(() => Close(commandId));
    }
}
