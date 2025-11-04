using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Flower.App.ViewModels;
using System;

namespace Flower.App.Windows;

public partial class ManageBusesWindow : Window
{
    public ManageBusesWindow() => InitializeComponent();

    public ManageBusesWindow(IAddOrUpdateFlowerViewModel vm) : this()
    {
        DataContext = vm;
        // Wire the named buttons (x:Name="AddButton" / "CancelButton")
        var addBtn = this.FindControl<Button>("ConfirmButton");
        var cancelBtn = this.FindControl<Button>("CancelButton");
        if (addBtn is not null) addBtn.Click += OnConfirmClicked;
        if (cancelBtn is not null) cancelBtn.Click += OnCancelClicked;
    }

    private void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is IAddOrUpdateFlowerViewModel vm)
                vm.ConfirmAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString()); // <- shows the exact “Unable to resolve ... while activating ...”
        }
    }


    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is IAddOrUpdateFlowerViewModel vm)
            vm.Cancel(); // keeps VM semantics

        Close(null);
    }
}