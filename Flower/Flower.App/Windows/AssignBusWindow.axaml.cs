using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Flower.App.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace Flower.App;

public partial class AssignBusWindow : Window
{
    private readonly AssignBusViewModel _vm;

    // Pass in busIds and (optionally) how many flowers you selected
    public AssignBusWindow(IEnumerable<string> busIds, int selectedCount = 0)
    {
        InitializeComponent();
        _vm = new AssignBusViewModel(busIds.ToList() ?? new List<string>(), selectedCount);
        DataContext = _vm;
        this.KeyDown += AssignBusWindow_KeyDown; // allow Esc to cancel
    }

    private void AssignBusWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close(null);
    }

    private void OnConfirmClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_vm.CanConfirm) Close(_vm.SelectedBusId);
    }
}