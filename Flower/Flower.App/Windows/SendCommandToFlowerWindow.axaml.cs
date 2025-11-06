using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Flower.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Flower.App.Windows;

public partial class SendCommandToFlowerWindow : Window
{
    private readonly ISendCommandToflowerViewModel _vm;


    public SendCommandToFlowerWindow(ISendCommandToflowerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;

        this.FindControl<Button>("ConfirmButton")!.Click += OnConfirmClicked;
        this.FindControl<Button>("CancelButton")!.Click += OnCancelClicked;
    }

    private async void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ISendCommandToflowerViewModel vm)
            await vm.ConfirmAsync(); // VM will raise CloseRequested -> window closes
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ISendCommandToflowerViewModel vm)
            vm.Cancel(); // CloseRequested(null)
        else
            Close(null);
    }
}