using Avalonia.Controls;
using Avalonia.Interactivity;
using Flower.App.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace Flower.App.Windows;

public partial class AssignBusWindow : Window
{
    private readonly IAssignBusViewModel _vm;

    public AssignBusWindow()
    {
        InitializeComponent();
    }

    public AssignBusWindow(IEnumerable<string> busIds, int selectedCount = 0) : this()
    {
        _vm = new AssignBusViewModel(busIds?.ToList() ?? new List<string>(), selectedCount);
        DataContext = _vm;

        var confirmButton = this.FindControl<Button>("ConfirmButton");
        var cancelBtn = this.FindControl<Button>("CancelButton");

        if (confirmButton is not null) confirmButton.Click += OnConfirmClicked;
        if (cancelBtn is not null) cancelBtn.Click += OnCancelClicked;
    }

    private void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is IAssignBusViewModel vm && _vm.CanConfirm)
            Close(_vm.SelectedBusId);
        else
            Close(null);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
        => Close(null);
}